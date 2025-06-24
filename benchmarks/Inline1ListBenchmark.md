BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                 | Count | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |------ |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| List_AddIterate        | 0     |   3.240 ns | 0.0887 ns | 0.1301 ns |  1.00 |    0.06 | 0.0051 |      32 B |        1.00 |
| Inline1List_AddIterate | 0     |   5.894 ns | 0.1141 ns | 0.1969 ns |  1.82 |    0.09 | 0.0051 |      32 B |        1.00 |
|                        |       |            |           |           |       |         |        |           |             |
| List_AddIterate        | 1     |   7.750 ns | 0.1803 ns | 0.3343 ns |  1.00 |    0.06 | 0.0102 |      64 B |        1.00 |
| Inline1List_AddIterate | 1     |   9.073 ns | 0.2013 ns | 0.2887 ns |  1.17 |    0.06 | 0.0051 |      32 B |        0.50 |
|                        |       |            |           |           |       |         |        |           |             |
| List_AddIterate        | 2     |  10.548 ns | 0.2342 ns | 0.3714 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| Inline1List_AddIterate | 2     |  20.572 ns | 0.2954 ns | 0.2467 ns |  1.95 |    0.07 | 0.0153 |      96 B |        1.50 |
|                        |       |            |           |           |       |         |        |           |             |
| List_AddIterate        | 16    |  55.197 ns | 1.0105 ns | 0.8958 ns |  1.00 |    0.02 | 0.0191 |     120 B |        1.00 |
| Inline1List_AddIterate | 16    | 149.599 ns | 1.2087 ns | 1.0094 ns |  2.71 |    0.05 | 0.0496 |     312 B |        2.60 |