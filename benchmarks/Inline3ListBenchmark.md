# Summary

| # Elements | Add + Iterate (Speed) | Memory Saved | Read (Speed) | Write (Speed) | InsertFront (Speed) | RemoveByValue (Speed) | RemoveFrontLoop (Speed) |
|-----------:|----------------------:|--------------|--------------|---------------|----------------------|------------------------|--------------------------|
| **0**      | 1.68× slower          | **+8 B**     | 1.61× slower | 1.21× slower  | 1.71× slower         | 1.15× slower           | 1.29× slower             |
| **1**      | **0.79× faster**      | **−24 B**    | 1.61× slower | 1.22× slower  | **0.69× faster**     | **≈1.75× faster**       | **≈1.7× faster**         |
| **2**      | **0.88× faster**      | **−24 B**    | 2.15× slower | 1.16× slower  | **0.62× faster**     | **≈1.85× faster**       | **≈2.1× faster**         |
| **3**      | ~equal (0.98×)        | +32 B        | 1.72× slower | 1.68× slower  | **0.51× faster**     | **≈2.8× faster**        | **≈2.8× faster**         |
| **4**      | 1.29× slower          | +40 B        | 2.16× slower | 1.66× slower  | **0.69× faster**     | **≈1.9× faster**        | **≈1.9× faster**         |
| **16**     | 1.75× slower          | +136 B       | 1.95× slower | 1.41× slower  | 1.21× slower         | ~1.1× faster            | 1.3× slower              |

---
BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                | Count | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ListAddAndIterate     | 0     |  3.299 ns | 0.0908 ns | 0.1682 ns |  3.241 ns |  1.00 |    0.07 | 0.0051 |      32 B |        1.00 |
| Inline3ListAddIterate | 0     |  5.536 ns | 0.1106 ns | 0.2427 ns |  5.498 ns |  1.68 |    0.11 | 0.0064 |      40 B |        1.25 |
|                       |       |           |           |           |           |       |         |        |           |             |
| ListAddAndIterate     | 1     |  7.499 ns | 0.1600 ns | 0.2081 ns |  7.457 ns |  1.00 |    0.04 | 0.0102 |      64 B |        1.00 |
| Inline3ListAddIterate | 1     |  5.906 ns | 0.0845 ns | 0.0705 ns |  5.923 ns |  0.79 |    0.02 | 0.0064 |      40 B |        0.62 |
|                       |       |           |           |           |           |       |         |        |           |             |
| ListAddAndIterate     | 2     | 10.225 ns | 0.2239 ns | 0.1869 ns | 10.199 ns |  1.00 |    0.02 | 0.0102 |      64 B |        1.00 |
| Inline3ListAddIterate | 2     |  8.967 ns | 0.2039 ns | 0.3462 ns |  9.006 ns |  0.88 |    0.04 | 0.0064 |      40 B |        0.62 |
|                       |       |           |           |           |           |       |         |        |           |             |
| ListAddAndIterate     | 3     | 12.926 ns | 0.2802 ns | 0.5398 ns | 12.851 ns |  1.00 |    0.06 | 0.0115 |      72 B |        1.00 |
| Inline3ListAddIterate | 3     | 12.604 ns | 0.2734 ns | 0.4092 ns | 12.629 ns |  0.98 |    0.05 | 0.0064 |      40 B |        0.56 |
|                       |       |           |           |           |           |       |         |        |           |             |
| ListAddAndIterate     | 4     | 18.498 ns | 0.6101 ns | 1.7894 ns | 17.898 ns |  1.01 |    0.14 | 0.0115 |      72 B |        1.00 |
| Inline3ListAddIterate | 4     | 23.721 ns | 0.4693 ns | 0.8814 ns | 23.603 ns |  1.29 |    0.13 | 0.0179 |     112 B |        1.56 |
|                       |       |           |           |           |           |       |         |        |           |             |
| ListAddAndIterate     | 16    | 52.314 ns | 0.9229 ns | 0.9478 ns | 52.143 ns |  1.00 |    0.02 | 0.0191 |     120 B |        1.00 |
| Inline3ListAddIterate | 16    | 91.399 ns | 1.8073 ns | 2.5336 ns | 90.943 ns |  1.75 |    0.06 | 0.0408 |     256 B |        2.13 |


---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method           | Count | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------- |------ |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| ListRead         | 1     |  0.3382 ns | 0.0315 ns | 0.0518 ns |  0.3414 ns |  1.02 |    0.21 |         - |          NA |
| Inline3ListRead  | 1     |  0.5342 ns | 0.0367 ns | 0.0478 ns |  0.5079 ns |  1.61 |    0.27 |         - |          NA |
| ListWrite        | 1     |  2.3139 ns | 0.0684 ns | 0.0889 ns |  2.3046 ns |  6.99 |    1.03 |         - |          NA |
| Inline3ListWrite | 1     |  2.8245 ns | 0.0588 ns | 0.0491 ns |  2.8139 ns |  8.53 |    1.23 |         - |          NA |
|                  |       |            |           |           |            |       |         |           |             |
| ListRead         | 2     |  0.8759 ns | 0.0427 ns | 0.0599 ns |  0.8729 ns |  1.00 |    0.09 |         - |          NA |
| Inline3ListRead  | 2     |  1.8713 ns | 0.0609 ns | 0.0911 ns |  1.8619 ns |  2.15 |    0.18 |         - |          NA |
| ListWrite        | 2     |  4.3894 ns | 0.1037 ns | 0.1349 ns |  4.4454 ns |  5.03 |    0.37 |         - |          NA |
| Inline3ListWrite | 2     |  5.1137 ns | 0.1254 ns | 0.1717 ns |  5.0985 ns |  5.86 |    0.43 |         - |          NA |
|                  |       |            |           |           |            |       |         |           |             |
| ListRead         | 3     |  1.7901 ns | 0.0595 ns | 0.0637 ns |  1.7986 ns |  1.00 |    0.05 |         - |          NA |
| Inline3ListRead  | 3     |  3.0700 ns | 0.0823 ns | 0.0980 ns |  3.1109 ns |  1.72 |    0.08 |         - |          NA |
| ListWrite        | 3     |  4.6152 ns | 0.1015 ns | 0.0949 ns |  4.5701 ns |  2.58 |    0.10 |         - |          NA |
| Inline3ListWrite | 3     |  7.7528 ns | 0.1779 ns | 0.2770 ns |  7.6913 ns |  4.34 |    0.21 |         - |          NA |
|                  |       |            |           |           |            |       |         |           |             |
| ListRead         | 4     |  2.3771 ns | 0.0701 ns | 0.0936 ns |  2.3681 ns |  1.00 |    0.05 |         - |          NA |
| Inline3ListRead  | 4     |  5.1184 ns | 0.1256 ns | 0.1719 ns |  5.1300 ns |  2.16 |    0.11 |         - |          NA |
| ListWrite        | 4     |  6.3512 ns | 0.1522 ns | 0.2278 ns |  6.2940 ns |  2.68 |    0.14 |         - |          NA |
| Inline3ListWrite | 4     | 10.7792 ns | 0.2378 ns | 0.2108 ns | 10.7165 ns |  4.54 |    0.20 |         - |          NA |
|                  |       |            |           |           |            |       |         |           |             |
| ListRead         | 16    | 10.3299 ns | 0.2262 ns | 0.3097 ns | 10.5078 ns |  1.00 |    0.04 |         - |          NA |
| Inline3ListRead  | 16    | 20.1184 ns | 0.4107 ns | 0.5341 ns | 19.9757 ns |  1.95 |    0.08 |         - |          NA |
| ListWrite        | 16    | 28.7536 ns | 0.5875 ns | 0.8611 ns | 28.6286 ns |  2.79 |    0.12 |         - |          NA |
| Inline3ListWrite | 16    | 41.8786 ns | 0.8184 ns | 1.0351 ns | 41.8170 ns |  4.06 |    0.16 |         - |          NA |

---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                  | Count | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |------ |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ListInsertFront         | 0     |   3.114 ns | 0.0889 ns | 0.2343 ns |   3.074 ns |  1.01 |    0.10 | 0.0051 |      32 B |        1.00 |
| Inline3List_InsertFront | 0     |   5.289 ns | 0.1055 ns | 0.2294 ns |   5.243 ns |  1.71 |    0.14 | 0.0064 |      40 B |        1.25 |
|                         |       |            |           |           |            |       |         |        |           |             |
| ListInsertFront         | 1     |   8.860 ns | 0.1974 ns | 0.3187 ns |   8.880 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| Inline3List_InsertFront | 1     |   6.124 ns | 0.1447 ns | 0.2337 ns |   6.053 ns |  0.69 |    0.04 | 0.0064 |      40 B |        0.62 |
|                         |       |            |           |           |            |       |         |        |           |             |
| ListInsertFront         | 2     |  14.284 ns | 0.3082 ns | 0.5938 ns |  14.375 ns |  1.00 |    0.06 | 0.0102 |      64 B |        1.00 |
| Inline3List_InsertFront | 2     |   8.785 ns | 0.2002 ns | 0.2806 ns |   8.884 ns |  0.62 |    0.03 | 0.0064 |      40 B |        0.62 |
|                         |       |            |           |           |            |       |         |        |           |             |
| ListInsertFront         | 3     |  26.451 ns | 0.5487 ns | 0.9015 ns |  25.922 ns |  1.00 |    0.05 | 0.0115 |      72 B |        1.00 |
| Inline3List_InsertFront | 3     |  13.462 ns | 0.2912 ns | 0.5325 ns |  13.681 ns |  0.51 |    0.03 | 0.0063 |      40 B |        0.56 |
|                         |       |            |           |           |            |       |         |        |           |             |
| ListInsertFront         | 4     |  38.301 ns | 0.7962 ns | 1.6083 ns |  37.575 ns |  1.00 |    0.06 | 0.0114 |      72 B |        1.00 |
| Inline3List_InsertFront | 4     |  26.349 ns | 0.4533 ns | 0.3785 ns |  26.195 ns |  0.69 |    0.03 | 0.0179 |     112 B |        1.56 |
|                         |       |            |           |           |            |       |         |        |           |             |
| ListInsertFront         | 16    | 179.424 ns | 3.5880 ns | 6.3777 ns | 177.180 ns |  1.00 |    0.05 | 0.0191 |     120 B |        1.00 |
| Inline3List_InsertFront | 16    | 217.334 ns | 4.2973 ns | 7.1799 ns | 214.381 ns |  1.21 |    0.06 | 0.0408 |     256 B |        2.13 |

---

BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4349/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.102
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method                                 | Count | Mean       | Error     | StdDev     | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------- |------ |-----------:|----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ListRemoveByValueSequential            | 0     |   2.916 ns | 0.0655 ns |  0.0701 ns |   2.890 ns |  1.00 |    0.03 | 0.0051 |      32 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 0     |   3.360 ns | 0.0923 ns |  0.1843 ns |   3.331 ns |  1.15 |    0.07 | 0.0064 |      40 B |        1.25 |
| ListRemoveFrontLoop                    | 0     |   4.383 ns | 0.0870 ns |  0.1778 ns |   4.404 ns |  1.50 |    0.07 | 0.0051 |      32 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 0     |   3.769 ns | 0.1009 ns |  0.1541 ns |   3.710 ns |  1.29 |    0.06 | 0.0064 |      40 B |        1.25 |
|                                        |       |            |           |            |            |       |         |        |           |             |
| ListRemoveByValueSequential            | 1     |  10.750 ns | 0.2346 ns |  0.3984 ns |  10.794 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 1     |   6.094 ns | 0.1472 ns |  0.3074 ns |   5.985 ns |  0.57 |    0.04 | 0.0064 |      40 B |        0.62 |
| ListRemoveFrontLoop                    | 1     |   8.344 ns | 0.1924 ns |  0.3419 ns |   8.236 ns |  0.78 |    0.04 | 0.0102 |      64 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 1     |   6.282 ns | 0.1487 ns |  0.2485 ns |   6.343 ns |  0.59 |    0.03 | 0.0064 |      40 B |        0.62 |
|                                        |       |            |           |            |            |       |         |        |           |             |
| ListRemoveByValueSequential            | 2     |  19.069 ns | 0.4006 ns |  0.6693 ns |  18.786 ns |  1.00 |    0.05 | 0.0102 |      64 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 2     |  10.261 ns | 0.2264 ns |  0.4362 ns |  10.044 ns |  0.54 |    0.03 | 0.0064 |      40 B |        0.62 |
| ListRemoveFrontLoop                    | 2     |  15.243 ns | 0.3281 ns |  0.6082 ns |  15.024 ns |  0.80 |    0.04 | 0.0102 |      64 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 2     |   9.022 ns | 0.2041 ns |  0.3521 ns |   8.918 ns |  0.47 |    0.02 | 0.0064 |      40 B |        0.62 |
|                                        |       |            |           |            |            |       |         |        |           |             |
| ListRemoveByValueSequential            | 3     |  38.314 ns | 0.7801 ns |  1.5579 ns |  38.077 ns |  1.00 |    0.06 | 0.0114 |      72 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 3     |  13.784 ns | 0.3321 ns |  0.9635 ns |  13.769 ns |  0.36 |    0.03 | 0.0063 |      40 B |        0.56 |
| ListRemoveFrontLoop                    | 3     |  25.148 ns | 0.5156 ns |  0.8027 ns |  24.901 ns |  0.66 |    0.03 | 0.0115 |      72 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 3     |  13.291 ns | 0.2733 ns |  0.2684 ns |  13.299 ns |  0.35 |    0.02 | 0.0063 |      40 B |        0.56 |
|                                        |       |            |           |            |            |       |         |        |           |             |
| ListRemoveByValueSequential            | 4     |  48.510 ns | 0.8045 ns |  0.8608 ns |  48.261 ns |  1.00 |    0.02 | 0.0114 |      72 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 4     |  25.890 ns | 0.5306 ns |  1.0095 ns |  25.670 ns |  0.53 |    0.02 | 0.0179 |     112 B |        1.56 |
| ListRemoveFrontLoop                    | 4     |  35.040 ns | 0.1842 ns |  0.1538 ns |  35.017 ns |  0.72 |    0.01 | 0.0114 |      72 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 4     |  25.174 ns | 0.5225 ns |  1.0674 ns |  24.873 ns |  0.52 |    0.02 | 0.0179 |     112 B |        1.56 |
|                                        |       |            |           |            |            |       |         |        |           |             |
| ListRemoveByValueSequential            | 16    | 257.407 ns | 5.1407 ns |  7.2065 ns | 256.376 ns |  1.00 |    0.04 | 0.0191 |     120 B |        1.00 |
| InlineThreeListRemoveByValueSequential | 16    | 230.681 ns | 4.6221 ns |  8.9052 ns | 227.700 ns |  0.90 |    0.04 | 0.0408 |     256 B |        2.13 |
| ListRemoveFrontLoop                    | 16    | 179.993 ns | 3.6089 ns |  6.6893 ns | 181.112 ns |  0.70 |    0.03 | 0.0191 |     120 B |        1.00 |
| InlineThreeListRemoveFrontLoop         | 16    | 235.965 ns | 4.7098 ns | 13.2839 ns | 235.698 ns |  0.92 |    0.06 | 0.0405 |     256 B |        2.13 |