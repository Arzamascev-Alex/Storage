```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-12450H 2.00GHz, 1 CPU, 12 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3


```
| Method                | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| SerializeJson         | 102.11 ns | 1.628 ns | 3.470 ns |  1.00 |    0.04 | 0.0139 |      - |      88 B |        1.00 |
| SerializeBinary       |  36.40 ns | 0.100 ns | 0.084 ns |  0.36 |    0.01 | 0.0688 | 0.0001 |     432 B |        4.91 |
| SerializeBinaryDirect |  10.44 ns | 0.091 ns | 0.081 ns |  0.10 |    0.00 | 0.0076 |      - |      48 B |        0.55 |
