using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Buffers;
using Storage.Core;
using System.Text.Json;
using System.Diagnostics;

namespace Storage.Server
{
    public sealed class TcpServer
    {
        private const int BufferSize  = 4096;
        private readonly IPEndPoint _endPoint = new(IPAddress.Loopback, 8080);
        private readonly SimpleStore _store;
        private const int MaxClients = 100;
        private readonly SemaphoreSlim _clientSemaphore = new(MaxClients, MaxClients);

        //  готовые ответы
        private static readonly byte[] OkResponse = Encoding.UTF8.GetBytes("OK\r\n");
        private static readonly byte[] NilResponse = Encoding.UTF8.GetBytes("(nil)\r\n");
        private static readonly byte[] InvalidCommandResponse = Encoding.UTF8.GetBytes("ERROR Invalid command\r\n");
        private static readonly byte[] UnknownCommandResponse = Encoding.UTF8.GetBytes("ERROR Unknown command\r\n");
        //private static readonly byte[] CommandTooLongResponse = Encoding.UTF8.GetBytes("ERROR Command too long\r\n");   //  ограничение для слишком длинных команд
        private static readonly byte[] InvalidJsonResponse = Encoding.UTF8.GetBytes("ERROR Invalid JSON\r\n");

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
                await _clientSemaphore.WaitAsync();

                Socket clientSocket;
                try
                {
                    clientSocket = await serverSocket.AcceptAsync();
                }
                catch
                {
                    _clientSemaphore.Release();
                    throw;
                }

                _ = ProcessClientAsync(clientSocket);
            }
        }

        private async Task ProcessClientAsync(Socket clientSocket)
        {
            byte[]? buffer = null;
            int bufferedCount = 0;

            try
            {
                buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                Console.WriteLine($"Client processing started: {clientSocket.RemoteEndPoint}");

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

                        byte[] response;

                        using (Activity? activity = ServerTelemetry.ActivitySource.StartActivity("storage.command", ActivityKind.Server))
                        {
                            activity?.SetTag("command.size", commandLength);

                            long startedAt = Stopwatch.GetTimestamp();

                            response = ExecuteCommand(buffer.AsSpan(commandStart, commandLength), activity);

                            double elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

                            ServerTelemetry.CommandsProcessed.Add(1);
                            ServerTelemetry.CommandDuration.Record(elapsedMs);
                        }

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
                        //await SendAllAsync(clientSocket, CommandTooLongResponse);
                        Console.WriteLine("Client disconnected: command size limit exceeded.");
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
                try
                {
                    if (buffer is not null)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    try
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException)
                    {
                        //
                    }
                }
                finally
                {
                    try
                    {
                        clientSocket.Dispose();
                    }
                    finally
                    {
                        _clientSemaphore.Release();
                    }
                }

                Console.WriteLine("Client socket close.");

            }

        }

        private byte[] ExecuteCommand(ReadOnlySpan<byte> commandBytes, Activity? activity)
        {
            Command command = CommandParser.Parse(commandBytes);

            activity?.SetTag("command.name", command.CommandName.IsEmpty ? "INVALID" : Encoding.UTF8.GetString(command.CommandName));

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

                UserProfile? profile;

                try
                {
                    profile = JsonSerializer.Deserialize<UserProfile>(command.Value);
                }
                catch (JsonException)
                {
                    return InvalidJsonResponse;
                }

                if (profile is null)
                {
                    return InvalidJsonResponse;
                }

                string key = Encoding.UTF8.GetString(command.Key);

                _store.Set(key, profile);

                return OkResponse;

            }

            if (command.CommandName.SequenceEqual("GET"u8))
            {
                if (!command.Value.IsEmpty)
                {
                    return InvalidCommandResponse;
                }

                string key = Encoding.UTF8.GetString(command.Key);

                UserProfile? profile = _store.Get(key);

                if (profile is null)
                {
                    return NilResponse;
                }

                byte[] value = JsonSerializer.SerializeToUtf8Bytes(profile);

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
