using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using InternalCollections;
using InternalCollections.Benchmarks;
using System.Runtime.InteropServices;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                 .Run(args);

public class Payload
{
    public int Value;
}

[Config(typeof(ColdVsHotConfig))]
[MemoryDiagnoser]
public class ArrayElementBenchmark
{
    [Params(8, 16)]
    public int Count;

    private Payload[] _plainArray = null!;
    private ArrayElement<Payload>[] _wrappedArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plainArray = new Payload[Count];
        for (var index = 0; index < Count; index++)
        {
            _plainArray[index] = new Payload { Value = index };
        }

        _wrappedArray = ArrayElement<Payload>.MakeElementArray(_plainArray)!;
    }

    [Benchmark(Baseline = true)]
    public int SumPlainArray()
    {
        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += _plainArray[index].Value;
        }

        return sum;
    }

    [Benchmark]
    public int SumWrappedArray()
    {
        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += _wrappedArray[index].Value.Value;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class InvariantArrayBenchmark
{
    [Params(8, 16)]
    public int Count;

    private InvariantArray<Payload> _invariantArray = null!;
    private Payload[] _rawArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        _invariantArray = new InvariantArray<Payload>(Count);
        _rawArray = new Payload[Count];

        for (var index = 0; index < Count; index++)
        {
            var payload = new Payload { Value = index };
            _invariantArray[index] = payload;
            _rawArray[index] = payload;
        }
    }

    [Benchmark(Baseline = true)]
    public int ReadRawArray()
    {
        var sum = 0;
        foreach (var element in _rawArray)
        {
            sum += element.Value;
        }

        return sum;
    }

    [Benchmark]
    public int ReadInvariantArray()
    {
        var sum = 0;
        foreach (var payload in _invariantArray)
        {
            sum += payload.Value;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class SpanListBenchmark
{
    [Params(8, 16)]
    public int Count;


    [Benchmark(Baseline = true)]
    public int ListAddAndIterate()
    {
        var integerList = new List<int>(Count);

        integerList.Clear();
        for (var index = 0; index < Count; index++)
        {
            integerList.Add(index);
        }

        var sum = 0;
        foreach (var value in integerList)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int SpanListAddAndIterate()
    {
        Span<int> buffer = stackalloc int[Count];
        var spanList = new SpanList<int>(buffer.Slice(0, Count));

        for (var index = 0; index < Count; index++)
        {
            spanList.Add(index);
        }

        var sum = 0;
        foreach (ref readonly var value in spanList)
        {
            sum += value;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class TinyDictionaryBenchmark
{
    [Params(8, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int DictionaryAddAndFind()
    {
        var dictionary = new Dictionary<int, int>(Count);

        for (var index = 0; index < Count; index++)
        {
            dictionary[index] = index;
        }

        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += dictionary[index];
        }

        return sum;
    }

    [Benchmark]
    public int TinyDictionaryAddAndFind()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[32];
        var tinyDictionary = new TinySpanDictionary<int, int>(buffer.Slice(0, Count));

        for (var index = 0; index < Count; index++)
        {
            tinyDictionary.AddOrSet(index, index);
        }

        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += tinyDictionary[index];
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class RefSpanBenchmark
{
    [Params(4, 8, 256)]
    public int Count;


    [Benchmark(Baseline = true)]
    public int ReadPlainArray()
    {
        var payloadArray = new Payload[Count];
        for (var index = 0; index < Count; index++)
        {
            payloadArray[index] = new Payload { Value = index };
        }

        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += payloadArray[index].Value;
        }

        return sum;
    }

    [Benchmark]
    public unsafe int ReadRefSpan()
    {
        var handles = stackalloc GCHandle[Count];
        using var referenceSpan = new RefSpan<Payload>(handles, Count);

        for (var index = 0; index < Count; index++)
        {
            referenceSpan[index] = new Payload { Value = index };
        }

        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += referenceSpan[index]!.Value;
        }

        return sum;
    }

}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class Inline1ListBenchmark
{
    [Params(0, 1, 2, 16)]
    public int Count;

    private List<int> _stdList = null!;
    private Inline1List<int> _inline1 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _stdList = new List<int>(Count);
        _inline1 = [];

        for (var i = 0; i < Count; i++)
        {
            _stdList.Add(i);
            _inline1.Add(i);
        }
    }

    [Benchmark(Baseline = true)]
    public int List_AddIterate()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }

    [Benchmark]
    public int Inline1List_AddIterate()
    {
        var list = new Inline1List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class Inline2ListBenchmark_Add
{
    [Params(0, 1, 2, 3, 16)]
    public int Count;

    private List<int> _stdList = null!;
    private Inline2List<int> _inline2 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _stdList = new List<int>(Count);
        _inline2 = [];

        for (var i = 0; i < Count; i++)
        {
            _stdList.Add(i);
            _inline2.Add(i);
        }
    }

    [Benchmark(Baseline = true)]
    public int List_AddIterate()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }

    [Benchmark]
    public int Inline2List_AddIterate()
    {
        var list = new Inline2List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class Inline2ListBenchmark_Insert
{
    [Params(0, 1, 2, 3, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int List_InsertFront()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }

    [Benchmark]
    public int Inline2_InsertFront()
    {
        var list = new Inline2List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        var sum = 0;
        foreach (var v in list)
        {
            sum += v;
        }

        return sum;
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class Inline2ListBenchmark_Indexer
{
    [Params(1, 2, 3, 16)]
    public int ElementCount;

    private List<int> _standardList = null!;
    private Inline2List<int> _inline2List = null!;

    [GlobalSetup]
    public void Setup()
    {
        _standardList = new List<int>(ElementCount);
        _inline2List = [];

        for (var index = 0; index < ElementCount; index++)
        {
            _standardList.Add(index);
            _inline2List.Add(index);
        }
    }

    [Benchmark(Baseline = true)]
    public int ListRead()
    {
        var sum = 0;

        for (var index = 0; index < ElementCount; index++)
        {
            sum += _standardList[index];
        }

        return sum;
    }

    [Benchmark]
    public int Inline2Read()
    {
        var sum = 0;

        for (var index = 0; index < ElementCount; index++)
        {
            sum += _inline2List[index];
        }

        return sum;
    }

    [Benchmark]
    public int ListWrite()
    {
        for (var index = 0; index < ElementCount; index++)
        {
            _standardList[index] = _standardList[index] + 1;
        }

        return _standardList[ElementCount - 1];
    }

    [Benchmark]
    public int Inline2Write()
    {
        for (var index = 0; index < ElementCount; index++)
        {
            _inline2List[index] = _inline2List[index] + 1;
        }

        return _inline2List[ElementCount - 1];
    }
}

[MemoryDiagnoser]
[Config(typeof(ColdVsHotConfig))]
public class Inline2ListBenchmark_Remove
{
    [Params(0, 1, 2, 3, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int List_RemoveByValueSequential()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            list.Remove(i);
        }

        return list.Count; 
    }

    [Benchmark]
    public int Inline2_RemoveByValueSequential()
    {
        var list = new Inline2List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            list.Remove(i);
        }

        return list.Count;
    }

    [Benchmark]
    public int List_RemoveFrontLoop()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        while (list.Count != 0)
        {
            list.RemoveAt(0);
        }

        return list.Count; 
    }

    [Benchmark]
    public int Inline2_RemoveFrontLoop()
    {
        var list = new Inline2List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        while (list.Count != 0)
        {
            list.RemoveAt(0);
        }

        return list.Count;
    }
}