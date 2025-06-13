# InternalCollections

Small, allocation-free helpers that appear as **internal** utilities in
performance-critical code. They live on the stack, rent buffers only when
necessary, and never resize implicit arrays behind your back.

> **Rule #1 – measure first.**  
> .NET 8/9’s JIT already eliminates many array-covariance checks and other
> overhead you might think you’re avoiding. Use these structs only if a profiler
> proves that stock `List<T>`, `Dictionary<TKey,TValue>`, or `ArrayPool<T>` is not
> fast enough.

---

## Library contents

| # | Type | Kind | What it does |
|---|------|------|--------------|
| 1 | **`ArrayElement<T>`** | `struct` | Wraps an array element to bypass covariance checks and costly struct returns. |
| 2 | **`InvariantArray<T>`** | `sealed class` | Fixed-size `IReadOnlyList<T>` over `ArrayElement<T>[]`; no covariance. |
| 3 | **`RefElement<T>`** | `ref struct` | Single-object pin/handle without heap allocations. |
| 4 | **`RefSpan<T>`** | `ref struct` | “Span” of `GCHandle`s for pinning many reference objects. |
| 5 | **`SpanList<T>`** | `ref struct` | Non-growing list over a caller-supplied `Span<T>`. |
| 6 | **`RentedList<T>`** | `readonly ref struct` | `List<T>` from a pool; auto-return on `Dispose()`. |
| 7 | **`ReRentableList<T>`** | `ref struct` | Like #6 but re-rents a *larger* list instead of letting `List<T>` reallocate its array. |
| 8 | **`HybridSpanRentList<T>`** | `ref struct` | Starts in `SpanList`; spills into `ReRentableList` when full. |
| 9 | **`TinySpanDictionary<TKey,TValue>`** | `ref struct` | Fixed-capacity map backed by a `Span<KeyValuePair<,>>`; ideal for ≤ 8 keys. |
|10 | **`SpanDictionary<TKey,TValue>`** | `ref struct` | True hash table over two spans (buckets + entries); never grows. |
|11 | **`RentedDictionary<TKey,TValue>`** | `readonly ref struct` | Pooled `Dictionary<,>` wrapper; auto-return on `Dispose()`. |
|12 | **`ReRentableDictionary<TKey,TValue>`** | `ref struct` | Pooled dictionary that re-rents a larger one when full. |
|13 | **`HybridSpanRentDictionary<TKey,TValue>`** | `ref struct` | Starts in a `SpanDictionary`; spills into a `ReRentableDictionary`. |

All structs are **allocation-free** and live on the stack (except the *rented*
parts that explicitly use pooling).

---

## Installation

```bash
dotnet add package InternalCollections
```

Targets `netstandard2.0`, `net8.0`, and `net9.0`.

## Quick examples

`ArrayElement<T>`:
```csharp
var raw = new[] { "one", "two", "three" };
ArrayElement<string>[] elements = ArrayElement<string>.MakeElementArray(raw);

elements[1].Value = "TWO";          // no covariance check
Console.WriteLine(elements[1]);     // implicit cast → "TWO"
```

`InvariantArray<T>`:
```csharp
var names = new InvariantArray<string>(3)
{
    [0] = "Alice",
    [1] = "Bob",
    [2] = "Eve"
};

foreach (var n in names)
{
    Console.WriteLine(n); // Alice Bob Eve
}
```

`RefElement<T>`:
```csharp
var element = new RefElement<byte[]>(new byte[256]);

Console.WriteLine(element.Value!.Length); // 256

element.Value = null;   // frees handle
```

`RefSpan<T>`:
```csharp
using System.Runtime.InteropServices;

unsafe
{
    GCHandle* handles = stackalloc GCHandle[2];
    var refs = new RefSpan<object>(handles, 2);

    refs[0] = new byte[256];        // pinned!
    refs[1] = "hello";

    Console.WriteLine(refs[1]);     // "hello"
    refs.Dispose();                 // frees all handles
}
```

`SpanList<T>`:
```csharp
Span<int> buf = stackalloc int[4];
var list = new SpanList<int>(buf);

list.AddRange([1, 2, 3, 4]);        // now full
Console.WriteLine(list.IsFull);     // True
```

`RentedList<T>`:
```csharp
using var temp = new RentedList<int>(capacity: 8);
temp.AddRange([10, 20, 30]);
Console.WriteLine(temp.Count); // 3
// auto-returned to pool at the end of scope
```

`ReRentableList<T>`:
```csharp
var grow = new ReRentableList<string>(capacity: 2);
grow.Add("A");
grow.Add("B");
grow.Add("C");                      // rents a larger list, no internal reallocation
Console.WriteLine(grow.Capacity);   // ≥ 4
grow.Dispose();
```

`HybridSpanRentList<T>`:
```csharp
Span<int> tiny = stackalloc int[3];
var list = new HybridSpanRentList<int>(tiny);

list.AddRange([1, 2, 3, 4, 5]);
Console.WriteLine(list.IsListRented); // True (spilled to pool)
```

`TinySpanDictionary<T>`:
```csharp
Span<KeyValuePair<int,string>> buffer = stackalloc KeyValuePair<int,char>[3];
var tiny = new TinySpanDictionary<int,char>(buffer);

tiny.Add(1, 'o');
tiny.Add(2, 't');
tiny.AddOrSet(1, 'u');            // overwrite

Console.WriteLine(tiny[1]);         // 'u'
```

`SpanDictionary<TKey, TValue>`:
```csharp
int size = HashHelpers.GetPrime(5);
Span<int> buckets = stackalloc int[size];
Span<HashEntry<char,int>> entries = stackalloc HashEntry<char,int>[size];

var dict = new SpanDictionary<char,int>(buckets, entries);
dict.Add('A', 100);
dict.Add('B', 200);

Console.WriteLine(dict.TryGetValue('B', out var v)); // True, v = 200
```

`RentedDictionary<TKey, TValue>`:
```csharp
using var map = new RentedDictionary<int,string>(capacity: 4);
map.Add(1, "one");
Console.WriteLine(map.ContainsKey(1));  // True
```

`ReRentableDictionary<TKey, TValue>`:
```csharp
var growMap = new ReRentableDictionary<string,int>(capacity: 2);
growMap.Add("A", 1);
growMap.Add("B", 2);
growMap.Add("C", 3);                  // re-rents larger dictionary
Console.WriteLine(growMap.Capacity);  // ≥ 4
growMap.Dispose();
```

`HybridSpanRentDictionary<TKey, TValue>`:
```csharp
int s = HashHelpers.GetPrime(2);
Span<int> b = stackalloc int[s];
Span<HashEntry<int,int>> e = stackalloc HashEntry<int,int>[s];

var hybrid = new HybridSpanRentDictionary<int,int>(b, e);
hybrid.Add(10, 100);   // span
hybrid.Add(20, 200);   // span
hybrid.Add(30, 300);   // pooled

Console.WriteLine(hybrid.IsDictionaryRented); // True
```