using System;
using System.IO;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Storage.Core;

namespace Storage.Benchmarks
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private readonly UserProfile _profile = new()
        {
            Id = 28,
            UserName = "Alex",
            CreatedAt = new DateTime(2026, 9, 26, 12, 30, 0, DateTimeKind.Utc)
        };

        [Benchmark(Baseline = true)]
        public byte[] SerializeJson()
        {
            return JsonSerializer.SerializeToUtf8Bytes(_profile);
        }

        [Benchmark]
        public byte[] SerializeBinary()
        {
            using var stream = new MemoryStream();

            _profile.SerializeToBinary(stream);

            return stream.ToArray();

        }

        [Benchmark]
        public byte[] SerializeBinaryDirect()
        {
            return _profile.SerializeToBinary();
        }

    }
}
