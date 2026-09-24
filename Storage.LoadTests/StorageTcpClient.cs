using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Storage.Core;
using System.Text.Json;

namespace Storage.LoadTests
{
    public sealed class StorageTcpClient : IDisposable
    {
        private const int MaxCommandSize = 4096;
        private readonly TcpClient _client = new();
        private NetworkStream? _stream;
        private StreamReader? _reader;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            await _client.ConnectAsync(host, port, cancellationToken);

            _stream = _client.GetStream();

            _reader = new StreamReader(_stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        }

        public async Task SetAsync(string key, UserProfile profile, CancellationToken cancellationToken = default)
        {
            ValidateKey(key);
            ArgumentNullException.ThrowIfNull(profile);

            string json = JsonSerializer.Serialize(profile);

            byte[] request = Encoding.UTF8.GetBytes($"SET {key} {json}\r\n");

            string response = await SendCommandAsync(request, cancellationToken);

            if (response != "OK")
            {
                throw new IOException($"Неожиданный ответ на SET: {response}");
            }
        }

        //public async Task SetAsync(string key, byte[] value, CancellationToken cancellationToken = default)
        //{
        //    ValidateKey(key);
        //    ArgumentNullException.ThrowIfNull(value);

        //    if (value.Length == 0 || Array.IndexOf(value, (byte)'\r') >= 0 || Array.IndexOf(value, (byte)'\n') >= 0)
        //    {
        //        throw new ArgumentException("Значение не должно быть пустым или содержать переносы строк.", nameof(value));
        //    }

        //    byte[] prefix = Encoding.UTF8.GetBytes($"SET {key} ");
        //    byte[] request = new byte[prefix.Length + value.Length + 2];

        //    prefix.CopyTo(request, 0 );
        //    value.CopyTo(request, prefix.Length);

        //    request[^2] = (byte)'\r';
        //    request[^1] = (byte)'\n';

        //    string response = await SendCommandAsync(request, cancellationToken);

        //    if (response != "OK")
        //    {
        //        throw new IOException($"Неожиданный ответ на SET: {response}");
        //    }

        //}

        public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            ValidateKey(key);

            byte[] request = Encoding.UTF8.GetBytes($"GET {key}\r\n");

            string response = await SendCommandAsync(request, cancellationToken);

            return response == "(nil)" ? null : response; 
        }

        private async Task<string> SendCommandAsync(byte[] request, CancellationToken cancellationToken)
        {
            if (_stream is null || _reader is null)
            {
                throw new InvalidOperationException("Сначала вызов ConnectAsync.");
            }

            if (request.Length > MaxCommandSize)
            {
                throw new ArgumentException($"Команда должна занимать не больше {MaxCommandSize} байт.");
            }

            await _stream.WriteAsync(request.AsMemory(), cancellationToken);

            string? response = await _reader.ReadLineAsync(cancellationToken);

            if (response is null)
            {
                throw new IOException("Сервер закрыл соединение, не прислав ответa.");
            }

            if (response.StartsWith("ERROR ", StringComparison.Ordinal))
            {
                throw new IOException($"Ошибка сервера: {response}");
            }

            return response;
        }

        private static void ValidateKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            foreach (char symbol in key)
            {
                if (char.IsWhiteSpace(symbol))
                {
                    throw new ArgumentException("Ключ не должен содержать пробелы или переносы строк.", nameof(key));
                }
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _client.Dispose();
        }

    }
}
