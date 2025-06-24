# Summary

| # Elements | Add + Iterate (Speed) | Memory Saved | RemoveByValueSequential (Speed) | InsertFront (Speed) |
|-----------:|----------------------|--------------|---------------------------------|---------------------|
| **0**      | 1.65× slower         | **+8 B**     | 1.12× slower                    | 1.38× slower        |
| **1**      | **0.79× faster**     | **−24 B**    | **≈2× faster**                  | **≈1.5× faster**    |
| **2**      | **0.80× faster**     | **−24 B**    | **≈2.5× faster**                | **≈1.6× faster**    |
| **3**      | 1.44× slower         | +40 B        | **≈2.3× faster**                | 1.2–1.3× faster     |
| **16**     | 1.73× slower         | +136 B       | **≈3.3× faster** (but `RemoveFront` ≈1.3× slower) | 1.22× slower |


---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                 | Count | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| List_AddIterate        | 0     |  3.139 ns | 0.0866 ns | 0.1669 ns |  1.00 |    0.07 | 0.0051 |      32 B |        1.00 |
| Inline2List_AddIterate | 0     |  5.153 ns | 0.0787 ns | 0.0657 ns |  1.65 |    0.09 | 0.0064 |      40 B |        1.25 |
|                        |       |           |           |           |       |         |        |           |             |
| List_AddIterate        | 1     |  7.526 ns | 0.1739 ns | 0.2200 ns |  1.00 |    0.04 | 0.0102 |      64 B |        1.00 |
| Inline2List_AddIterate | 1     |  5.919 ns | 0.1381 ns | 0.1357 ns |  0.79 |    0.03 | 0.0064 |      40 B |        0.62 |
|                        |       |           |           |           |       |         |        |           |             |
| List_AddIterate        | 2     | 10.551 ns | 0.2352 ns | 0.4587 ns |  1.00 |    0.06 | 0.0102 |      64 B |        1.00 |
| Inline2List_AddIterate | 2     |  8.474 ns | 0.1907 ns | 0.2610 ns |  0.80 |    0.04 | 0.0064 |      40 B |        0.62 |
|                        |       |           |           |           |       |         |        |           |             |
| List_AddIterate        | 3     | 13.446 ns | 0.2895 ns | 0.5646 ns |  1.00 |    0.06 | 0.0115 |      72 B |        1.00 |
| Inline2List_AddIterate | 3     | 19.357 ns | 0.3929 ns | 0.6117 ns |  1.44 |    0.08 | 0.0179 |     112 B |        1.56 |
|                        |       |           |           |           |       |         |        |           |             |
| List_AddIterate        | 16    | 52.996 ns | 0.7694 ns | 0.6821 ns |  1.00 |    0.02 | 0.0191 |     120 B |        1.00 |
| Inline2List_AddIterate | 16    | 91.924 ns | 1.8556 ns | 2.4772 ns |  1.73 |    0.05 | 0.0408 |     256 B |        2.13 |

---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method       | ElementCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |------------- |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| ListRead     | 1            |  0.3450 ns | 0.0324 ns | 0.0421 ns |  0.3267 ns |  1.01 |    0.16 |         - |          NA |
| Inline2Read  | 1            |  0.4855 ns | 0.0168 ns | 0.0141 ns |  0.4844 ns |  1.43 |    0.16 |         - |          NA |
| ListWrite    | 1            |  2.8589 ns | 0.0804 ns | 0.0957 ns |  2.8106 ns |  8.39 |    0.95 |         - |          NA |
| Inline2Write | 1            |  2.8406 ns | 0.0784 ns | 0.0871 ns |  2.8260 ns |  8.34 |    0.94 |         - |          NA |
|              |              |            |           |           |            |       |         |           |             |
| ListRead     | 2            |  1.6922 ns | 0.0605 ns | 0.0959 ns |  1.7504 ns |  1.00 |    0.08 |         - |          NA |
| Inline2Read  | 2            |  2.0951 ns | 0.0569 ns | 0.0475 ns |  2.0920 ns |  1.24 |    0.08 |         - |          NA |
| ListWrite    | 2            |  3.7900 ns | 0.0614 ns | 0.0513 ns |  3.7637 ns |  2.25 |    0.13 |         - |          NA |
| Inline2Write | 2            |  4.8840 ns | 0.0491 ns | 0.0383 ns |  4.8740 ns |  2.90 |    0.17 |         - |          NA |
|              |              |            |           |           |            |       |         |           |             |
| ListRead     | 3            |  2.2024 ns | 0.0577 ns | 0.0482 ns |  2.1832 ns |  1.00 |    0.03 |         - |          NA |
| Inline2Read  | 3            |  3.4753 ns | 0.0930 ns | 0.1580 ns |  3.4468 ns |  1.58 |    0.08 |         - |          NA |
| ListWrite    | 3            |  5.8336 ns | 0.0767 ns | 0.0599 ns |  5.8500 ns |  2.65 |    0.06 |         - |          NA |
| Inline2Write | 3            |  7.6662 ns | 0.1733 ns | 0.2848 ns |  7.6102 ns |  3.48 |    0.15 |         - |          NA |
|              |              |            |           |           |            |       |         |           |             |
| ListRead     | 16           |  9.7064 ns | 0.2196 ns | 0.3219 ns |  9.5506 ns |  1.00 |    0.05 |         - |          NA |
| Inline2Read  | 16           | 16.6805 ns | 0.2585 ns | 0.2159 ns | 16.5788 ns |  1.72 |    0.06 |         - |          NA |
| ListWrite    | 16           | 27.1946 ns | 0.5097 ns | 0.4768 ns | 27.3374 ns |  2.80 |    0.10 |         - |          NA |
| Inline2Write | 16           | 38.9464 ns | 0.7996 ns | 0.9519 ns | 39.4349 ns |  4.02 |    0.16 |         - |          NA |

---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method              | Count | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |------ |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| List_InsertFront    | 0     |   2.964 ns | 0.0823 ns | 0.1605 ns |   2.973 ns |  1.00 |    0.08 | 0.0051 |      32 B |        1.00 |
| Inline2_InsertFront | 0     |   4.068 ns | 0.1051 ns | 0.1895 ns |   4.107 ns |  1.38 |    0.10 | 0.0064 |      40 B |        1.25 |
|                     |       |            |           |           |            |       |         |        |           |             |
| List_InsertFront    | 1     |   8.782 ns | 0.1948 ns | 0.3561 ns |   8.865 ns |  1.00 |    0.06 | 0.0102 |      64 B |        1.00 |
| Inline2_InsertFront | 1     |   6.044 ns | 0.1430 ns | 0.2389 ns |   5.938 ns |  0.69 |    0.04 | 0.0064 |      40 B |        0.62 |
|                     |       |            |           |           |            |       |         |        |           |             |
| List_InsertFront    | 2     |  14.301 ns | 0.3093 ns | 0.5497 ns |  14.473 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| Inline2_InsertFront | 2     |   8.930 ns | 0.2020 ns | 0.2896 ns |   8.961 ns |  0.63 |    0.03 | 0.0064 |      40 B |        0.62 |
|                     |       |            |           |           |            |       |         |        |           |             |
| List_InsertFront    | 3     |  26.218 ns | 0.5362 ns | 0.7340 ns |  25.916 ns |  1.00 |    0.04 | 0.0115 |      72 B |        1.00 |
| Inline2_InsertFront | 3     |  20.992 ns | 0.4413 ns | 0.7729 ns |  20.700 ns |  0.80 |    0.04 | 0.0179 |     112 B |        1.56 |
|                     |       |            |           |           |            |       |         |        |           |             |
| List_InsertFront    | 16    | 179.072 ns | 3.5776 ns | 7.3081 ns | 180.352 ns |  1.00 |    0.06 | 0.0191 |     120 B |        1.00 |
| Inline2_InsertFront | 16    | 217.482 ns | 4.3446 ns | 7.4943 ns | 214.111 ns |  1.22 |    0.06 | 0.0408 |     256 B |        2.13 |

---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                          | Count | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------ |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| List_RemoveByValueSequential    | 0     |   2.986 ns | 0.0835 ns | 0.1798 ns |  1.00 |    0.08 | 0.0051 |      32 B |        1.00 |
| Inline2_RemoveByValueSequential | 0     |   3.324 ns | 0.0900 ns | 0.0963 ns |  1.12 |    0.07 | 0.0064 |      40 B |        1.25 |
| List_RemoveFrontLoop            | 0     |   3.102 ns | 0.0860 ns | 0.1551 ns |  1.04 |    0.08 | 0.0051 |      32 B |        1.00 |
| Inline2_RemoveFrontLoop         | 0     |   3.984 ns | 0.0979 ns | 0.1340 ns |  1.34 |    0.09 | 0.0064 |      40 B |        1.25 |
|                                 |       |            |           |           |       |         |        |           |             |
| List_RemoveByValueSequential    | 1     |  10.413 ns | 0.2323 ns | 0.3548 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| Inline2_RemoveByValueSequential | 1     |   5.351 ns | 0.1295 ns | 0.1330 ns |  0.51 |    0.02 | 0.0064 |      40 B |        0.62 |
| List_RemoveFrontLoop            | 1     |   8.536 ns | 0.1917 ns | 0.2424 ns |  0.82 |    0.04 | 0.0102 |      64 B |        1.00 |
| Inline2_RemoveFrontLoop         | 1     |   5.646 ns | 0.1314 ns | 0.2594 ns |  0.54 |    0.03 | 0.0064 |      40 B |        0.62 |
|                                 |       |            |           |           |       |         |        |           |             |
| List_RemoveByValueSequential    | 2     |  20.107 ns | 0.4258 ns | 0.7114 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| Inline2_RemoveByValueSequential | 2     |   7.776 ns | 0.1759 ns | 0.1806 ns |  0.39 |    0.02 | 0.0064 |      40 B |        0.62 |
| List_RemoveFrontLoop            | 2     |  14.970 ns | 0.3183 ns | 0.4249 ns |  0.75 |    0.03 | 0.0102 |      64 B |        1.00 |
| Inline2_RemoveFrontLoop         | 2     |   9.039 ns | 0.2045 ns | 0.3891 ns |  0.45 |    0.02 | 0.0064 |      40 B |        0.62 |
|                                 |       |            |           |           |       |         |        |           |             |
| List_RemoveByValueSequential    | 3     |  34.940 ns | 0.7250 ns | 1.1708 ns |  1.00 |    0.05 | 0.0114 |      72 B |        1.00 |
| Inline2_RemoveByValueSequential | 3     |  15.486 ns | 0.3084 ns | 0.4323 ns |  0.44 |    0.02 | 0.0179 |     112 B |        1.56 |
| List_RemoveFrontLoop            | 3     |  30.954 ns | 1.5237 ns | 4.4926 ns |  0.89 |    0.13 | 0.0115 |      72 B |        1.00 |
| Inline2_RemoveFrontLoop         | 3     |  22.952 ns | 0.6241 ns | 1.7499 ns |  0.66 |    0.05 | 0.0178 |     112 B |        1.56 |
|                                 |       |            |           |           |       |         |        |           |             |
| List_RemoveByValueSequential    | 16    | 255.756 ns | 5.1150 ns | 6.6510 ns |  1.00 |    0.04 | 0.0191 |     120 B |        1.00 |
| Inline2_RemoveByValueSequential | 16    |  75.983 ns | 1.5202 ns | 1.9226 ns |  0.30 |    0.01 | 0.0408 |     256 B |        2.13 |
| List_RemoveFrontLoop            | 16    | 176.313 ns | 3.5031 ns | 4.6766 ns |  0.69 |    0.03 | 0.0191 |     120 B |        1.00 |
| Inline2_RemoveFrontLoop         | 16    | 229.262 ns | 4.4800 ns | 6.8414 ns |  0.90 |    0.03 | 0.0408 |     256 B |        2.13 |