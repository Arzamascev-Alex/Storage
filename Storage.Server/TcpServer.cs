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

        private static async Task ProcessClientAsync(Socket clientSocket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                while (true)
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected.");
                        break;
                    }

                    ReadOnlySpan<byte> receivedData = buffer.AsSpan(0, bytesRead);

                    ParsedCommand parsedCommand = CommandParser.Parse(receivedData);

                    Console.WriteLine($"Command: {Encoding.UTF8.GetString(parsedCommand.Command)}");
                    Console.WriteLine($"Key: {Encoding.UTF8.GetString(parsedCommand.Key)}");
                    Console.WriteLine($"Value: {Encoding.UTF8.GetString(parsedCommand.Value)}");
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

    }
}
