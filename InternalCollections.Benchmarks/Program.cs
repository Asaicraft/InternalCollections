using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using InternalCollections;
using InternalCollections.Benchmarks;
using System.Runtime.InteropServices;

var result = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                 .Run(args);

public class Payload
{
    public int Value;
}

public sealed class DerivedPayload : Payload
{
    public int Extra;
}

[MemoryDiagnoser]
public class ArrayElementBenchmark
{
    [Params(32, 64, 128)] 
    public int Count;

    private Payload[] _plainArray = null!;
    private ArrayElement<Payload>[] _wrappedArray = null!;
    private DerivedPayload[] _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plainArray = new Payload[Count];
        _wrappedArray = ArrayElement<Payload>.MakeElementArray(_plainArray)!;

        _source = new DerivedPayload[Count];
        for (var i = 0; i < Count; i++)
        {
            _source[i] = new DerivedPayload { Value = i, Extra = ~i };
        }
    }

    [Benchmark(Baseline = true)]
    public void FillPlainArray()
    {
        for (var i = 0; i < Count; i++)
        {
            _plainArray[i] = _source[i];
        }
    }
    [Benchmark]
    public void FillWrappedArray()
    {
        for (var i = 0; i < Count; i++)
        {
            _wrappedArray[i].Value = _source[i];
        }
    }
}

[MemoryDiagnoser]
public class InvariantArrayBenchmark
{
    [Params(32, 64, 128)]
    public int Count;

    private InvariantArray<Payload> _invariantArray = null!;
    private Payload[] _plainArray = null!;
    private DerivedPayload[] _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        _invariantArray = new InvariantArray<Payload>(Count);
        _plainArray = new Payload[Count];

        _source = new DerivedPayload[Count];
        for (var i = 0; i < Count; i++)
        {
            _source[i] = new DerivedPayload { Value = i, Extra = ~i };
        }
    }

    [Benchmark(Baseline = true)]
    public void FillPlainArray()
    {
        for (var i = 0; i < Count; i++)
        {
            _plainArray[i] = _source[i];
        }
    }
    [Benchmark]
    public void FillWrappedArray()
    {
        for (var i = 0; i < Count; i++)
        {
            _invariantArray[i] = _source[i];
        }
    }
}

[MemoryDiagnoser]
public class SpanListBenchmark
{
    [Params(8, 16, 256, 1024)]
    public int Count;

    private int[] _randomIndices;
    private Random _random;

    [GlobalSetup]
    public void Setup()
    {
        _random = new Random(42);
        _randomIndices = new int[Count];

        for (var i = 0; i < Count; i++)
        {
            _randomIndices[i] = _random.Next(0, i + 1);
        }
    }

    [Benchmark(Baseline = true)]
    public int List_AddIterate() => AddIterate_List();
    [Benchmark]
    public int Span_AddIterate() => AddIterate_Span();

    [Benchmark]
    public void List_InsertMiddle() => InsertMiddle_List();
    [Benchmark]
    public void Span_InsertMiddle() => InsertMiddle_Span();

    [Benchmark]
    public void List_RandomRemove() => RandomRemove_List();
    [Benchmark]
    public void Span_RandomRemove() => RandomRemove_Span();

    [Benchmark]
    public void List_RandomInsert() => RandomInsert_List();
    [Benchmark]
    public void Span_RandomInsert() => RandomInsert_Span();

    [Benchmark]
    public void List_Contains() => Contains_List();
    [Benchmark]
    public void Span_Contains() => Contains_Span();

    [Benchmark]
    public void List_IndexOf() => IndexOf_List();
    [Benchmark]
    public void Span_IndexOf() => IndexOf_Span();

    [Benchmark]
    public void List_ConcurrentAddIterate() => ConcurrentAdd_List();
    [Benchmark]
    public void Span_ConcurrentAddIterate() => ConcurrentAdd_Span();

    private int AddIterate_List()
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

    private int AddIterate_Span()
    {
        Span<int> buffer = stackalloc int[Count];
        var spanList = new SpanList<int>(buffer);
        for (var i = 0; i < Count; i++)
        {
            spanList.Add(i);
        }

        var sum = 0;
        foreach (ref readonly var v in spanList)
        {
            sum += v;
        }

        return sum;
    }

    private void InsertMiddle_List()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Insert(list.Count / 2, i);
        }
    }

    private void InsertMiddle_Span()
    {
        Span<int> buf = stackalloc int[Count];
        var spanList = new SpanList<int>(buf);
        for (var i = 0; i < Count; i++)
        {
            spanList.Insert(spanList.Count / 2, i);
        }
    }

    private void RandomRemove_List()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            var index = _randomIndices[i] % list.Count;
            list.RemoveAt(index);
        }
    }

    private void RandomRemove_Span()
    {
        Span<int> buffer = stackalloc int[Count];
        var spanList = new SpanList<int>(buffer);
        for (var i = 0; i < Count; i++)
        {
            spanList.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            var index = _randomIndices[i] % spanList.Count;
            spanList.RemoveAt(index);
        }
    }

    private void RandomInsert_List()
    {
        var list = new List<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            var index = _randomIndices[i] % (list.Count + 1);
            list.Insert(index, i);
        }
    }

    private void RandomInsert_Span()
    {
        Span<int> buf = stackalloc int[Count];
        var spanList = new SpanList<int>(buf);
        for (var i = 0; i < Count; i++)
        {
            var index = _randomIndices[i] % (spanList.Count + 1);
            spanList.Insert(index, i);
        }
    }

    private void Contains_List()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }
        for (var i = 0; i < Count; i++)
        {
            _ = list.Contains(_randomIndices[i] % Count);
        }
    }

    private void Contains_Span()
    {
        Span<int> buffer = stackalloc int[Count];
        var spanList = new SpanList<int>(buffer);
        for (var i = 0; i < Count; i++)
        {
            spanList.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            _ = spanList.Contains(_randomIndices[i] % Count);
        }
    }

    private void IndexOf_List()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        for (var i = 0; i < Count; i++)
        {
            _ = list.IndexOf(_randomIndices[i] % Count);
        }
    }

    private void IndexOf_Span()
    {
        Span<int> buf = stackalloc int[Count];
        var spanList = new SpanList<int>(buf);
        for (var i = 0; i < Count; i++)
        {
            spanList.Add(i);
        }
        for (var i = 0; i < Count; i++)
        {
            _ = spanList.IndexOf(_randomIndices[i] % Count);
        }
    }

    private void ConcurrentAdd_List()
    {
        Parallel.For(0, Environment.ProcessorCount, _ => AddIterate_List());
    }

    private void ConcurrentAdd_Span()
    {
        Parallel.For(0, Environment.ProcessorCount, _ => AddIterate_Span());
    }
}


[MemoryDiagnoser]
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

[MemoryDiagnoser]
public class Inline3ListBenchmark_Add
{
    [Params(0, 1, 2, 3, 4, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int ListAddAndIterate()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var value in list)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Inline3ListAddIterate()
    {
        var list = new Inline3List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        var sum = 0;
        foreach (var value in list)
        {
            sum += value;
        }

        return sum;
    }
}

[MemoryDiagnoser]
public class Inline3ListBenchmark_Insert
{
    [Params(0, 1, 2, 3, 4, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int ListInsertFront()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        var sum = 0;
        foreach (var value in list)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Inline3List_InsertFront()
    {
        var list = new Inline3List<int>();
        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        var sum = 0;
        foreach (var value in list)
        {
            sum += value;
        }

        return sum;
    }
}

[MemoryDiagnoser]
public class Inline3ListBenchmark_Indexer
{
    [Params(1, 2, 3, 4, 16)]
    public int Count;

    private List<int> _standardList = null!;
    private Inline3List<int> _inlineThreeList = null!;

    [GlobalSetup]
    public void Setup()
    {
        _standardList = new List<int>(Count);
        _inlineThreeList = [];

        for (var i = 0; i < Count; i++)
        {
            _standardList.Add(i);
            _inlineThreeList.Add(i);
        }
    }

    [Benchmark(Baseline = true)]
    public int ListRead()
    {
        var sum = 0;
        for (var i = 0; i < Count; i++)
        {
            sum += _standardList[i];
        }

        return sum;
    }

    [Benchmark]
    public int Inline3ListRead()
    {
        var sum = 0;
        for (var i = 0; i < Count; i++)
        {
            sum += _inlineThreeList[i];
        }

        return sum;
    }

    [Benchmark]
    public int ListWrite()
    {
        for (var i = 0; i < Count; i++)
        {
            _standardList[i] += 1;
        }

        return _standardList[Count - 1];
    }

    [Benchmark]
    public int Inline3ListWrite()
    {
        for (var i = 0; i < Count; i++)
        {
            _inlineThreeList[i] += 1;
        }

        return _inlineThreeList[Count - 1];
    }
}

[MemoryDiagnoser]
public class Inline3ListBenchmarkRemove
{
    [Params(0, 1, 2, 3, 4, 16)]
    public int Count;

    [Benchmark(Baseline = true)]
    public int ListRemoveByValueSequential()
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
    public int InlineThreeListRemoveByValueSequential()
    {
        var list = new Inline3List<int>();
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
    public int ListRemoveFrontLoop()
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
    public int InlineThreeListRemoveFrontLoop()
    {
        var list = new Inline3List<int>();
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