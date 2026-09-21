using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Buffers;
using Storage.Core;

namespace Storage.Server
{
    public sealed class TcpServer
    {
        private const int BufferSize  = 4096;
        private readonly IPEndPoint _endPoint = new(IPAddress.Loopback, 8080);
        private readonly SimpleStore _store; 

        //  готовые ответы
        private static readonly byte[] OkResponse = Encoding.UTF8.GetBytes("OK\r\n");
        private static readonly byte[] NilResponse = Encoding.UTF8.GetBytes("(nil)\r\n");
        private static readonly byte[] InvalidCommandResponse = Encoding.UTF8.GetBytes("ERROR Invalid command\r\n");
        private static readonly byte[] UnknownCommandResponse = Encoding.UTF8.GetBytes("ERROR Unknown command\r\n");
        private static readonly byte[] CommandTooLongResponse = Encoding.UTF8.GetBytes("ERROR Command too long\r\n");   //  ограничение для слишком длинных команд

        public TcpServer(SimpleStore store)
        {
            _store = store;
        }

        public async Task StartAsync()
        {
            using Socket serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(_endPoint);
            serverSocket.Listen(100);

            Console.WriteLine($"Server started on {_endPoint}");

            while (true)
            {
                Socket clientSocket = await serverSocket.AcceptAsync();
                Console.WriteLine($"CLient connected: {clientSocket.RemoteEndPoint}");

                _ = ProcessClientAsync(clientSocket);
            }
        }

        private async Task ProcessClientAsync(Socket clientSocket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            int bufferedCount = 0;

            try
            {
                while (true)
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer.AsMemory(bufferedCount, BufferSize - bufferedCount), SocketFlags.None);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected.");
                        break;
                    }

                    bufferedCount += bytesRead;

                    //  upd: клиент завершает каждый запрос символом \n или парой \r\n 
                    //  без них сервер ждёт продолжения, при отключении клиента незавершённую строку не выполняем

                    int commandStart = 0;

                    while (true)
                    {
                        int newLineIndex = Array.IndexOf(buffer, (byte)'\n', commandStart, bufferedCount - commandStart);

                        if (newLineIndex < 0)
                        {
                            break;
                        }

                        int commandLength = newLineIndex - commandStart;

                        if (commandLength > 0 && buffer[newLineIndex - 1] == (byte)'\r')    //  исключаем  \r
                        {
                            commandLength--;
                        }

                        byte[] response = ExecuteCommand(buffer.AsSpan(commandStart, commandLength));

                        await SendAllAsync(clientSocket, response);

                        commandStart = newLineIndex + 1;    // следующая команда начинется после \n
                    }

                    int remainingCount = bufferedCount - commandStart;

                    if (remainingCount > 0 && commandStart > 0)
                    {
                        Array.Copy(buffer, commandStart, buffer, 0, remainingCount);
                    }

                    bufferedCount = remainingCount;

                    if (bufferedCount == BufferSize)
                    {
                        await SendAllAsync(clientSocket, CommandTooLongResponse);
                        break;
                    }

                    //ReadOnlySpan<byte> receivedData = buffer.AsSpan(0, bytesRead);

                    //Command parsedCommand = CommandParser.Parse(receivedData);

                    //Console.WriteLine($"Command: {Encoding.UTF8.GetString(parsedCommand.CommandName)}");
                    //Console.WriteLine($"Key: {Encoding.UTF8.GetString(parsedCommand.Key)}");
                    //Console.WriteLine($"Value: {Encoding.UTF8.GetString(parsedCommand.Value)}");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketError: {ex.Message}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);

                try 
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    //
                }

                clientSocket.Dispose();
                Console.WriteLine("Client socket close.");
            }

        }

        private byte[] ExecuteCommand(ReadOnlySpan<byte> commandBytes)
        {
            Command command = CommandParser.Parse(commandBytes);

            if (command.CommandName.IsEmpty || command.Key.IsEmpty)
            {
                return InvalidCommandResponse;
            }

            if (command.CommandName.SequenceEqual("SET"u8))
            {
                if (command.Value.IsEmpty)
                {
                    return InvalidCommandResponse;
                }

                string key = Encoding.UTF8.GetString(command.Key);
                byte[] value = command.Value.ToArray();

                _store.Set(key, value);

                return OkResponse;

            }

            if (command.CommandName.SequenceEqual("GET"u8))
            {
                if (!command.Value.IsEmpty)
                {
                    return InvalidCommandResponse;
                }

                string key = Encoding.UTF8.GetString(command.Key);
                byte[]? value = _store.Get(key);

                if (value is null)
                {
                    return NilResponse;
                }

                byte[] responce = new byte[value.Length + 2];

                value.AsSpan().CopyTo(responce.AsSpan());

                responce[^2] = (byte)'\r';
                responce[^1] = (byte)'\n';

                return responce;
            }

            if (command.CommandName.SequenceEqual("DEL"u8) || command.CommandName.SequenceEqual("DELETE"u8))
            {
                if (!command.Value.IsEmpty)
                {
                    return InvalidCommandResponse;
                }

                string key = Encoding.UTF8.GetString(command.Key);

                _store.Delete(key);

                return OkResponse;
            }

            return UnknownCommandResponse;

        }

        private static async Task SendAllAsync(Socket clientSocket, ReadOnlyMemory<byte> responce)
        {
            while (!responce.IsEmpty)
            {
                int bytesSend = await clientSocket.SendAsync(responce, SocketFlags.None);

                if (bytesSend == 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }

                responce = responce.Slice(bytesSend);   //  оставляет участок после отправленных байтов без копирования
            }

        }

    }
}
