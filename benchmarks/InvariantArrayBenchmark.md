BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method           | Count | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------- |------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| FillPlainArray   | 32    | 182.77 ns |  1.837 ns |  1.628 ns |  1.00 |    0.01 |         - |          NA |
| FillWrappedArray | 32    |  57.55 ns |  1.180 ns |  1.906 ns |  0.31 |    0.01 |         - |          NA |
|                  |       |           |           |           |       |         |           |             |
| FillPlainArray   | 64    | 367.57 ns |  7.318 ns | 11.175 ns |  1.00 |    0.04 |         - |          NA |
| FillWrappedArray | 64    | 120.05 ns |  2.436 ns |  4.515 ns |  0.33 |    0.02 |         - |          NA |
|                  |       |           |           |           |       |         |           |             |
| FillPlainArray   | 128   | 744.93 ns | 14.590 ns | 26.308 ns |  1.00 |    0.05 |         - |          NA |
| FillWrappedArray | 128   | 228.80 ns |  3.648 ns |  4.201 ns |  0.31 |    0.01 |         - |          NA |