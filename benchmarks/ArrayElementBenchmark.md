BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method           | Count | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------- |------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| FillPlainArray   | 32    | 162.01 ns |  3.218 ns |  5.104 ns |  1.00 |    0.04 |         - |          NA |
| FillWrappedArray | 32    |  63.70 ns |  1.289 ns |  1.764 ns |  0.39 |    0.02 |         - |          NA |
|                  |       |           |           |           |       |         |           |             |
| FillPlainArray   | 64    | 315.13 ns |  6.335 ns |  7.779 ns |  1.00 |    0.03 |         - |          NA |
| FillWrappedArray | 64    | 127.60 ns |  2.060 ns |  1.927 ns |  0.41 |    0.01 |         - |          NA |
|                  |       |           |           |           |       |         |           |             |
| FillPlainArray   | 128   | 633.08 ns | 12.296 ns | 19.144 ns |  1.00 |    0.04 |         - |          NA |
| FillWrappedArray | 128   | 247.10 ns |  2.482 ns |  1.938 ns |  0.39 |    0.01 |         - |          NA |