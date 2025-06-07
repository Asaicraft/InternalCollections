// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using InternalCollections;
using InternalCollections.Benchmarks;

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
            _rawArray[index]= payload;
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
        Span<int> buffer = stackalloc int[32];
        var spanList = new SpanList<int>(buffer[..Count]);

        for (var index = 0; index < Count; index++)
        {
            spanList.Add(index);
        }

        var sum = 0;
        foreach (ref readonly var value in spanList.AsReadOnlySpan())
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
        var tinyDictionary = new TinySpanDictionary<int, int>(buffer[..Count]);

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
    [Params(4, 8)]
    public int Count;

    private Payload[] _payloadArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        _payloadArray = new Payload[Count];
        for (var index = 0; index < Count; index++)
        {
            _payloadArray[index] = new Payload { Value = index };
        }
    }

    [Benchmark(Baseline = true)]
    public int ReadPlainArray()
    {
        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += _payloadArray[index].Value;
        }

        return sum;
    }

    [Benchmark]
    public int ReadRefSpan()
    {
        Span<IntPtr> handles = stackalloc IntPtr[32];
        using var referenceSpan = new RefSpan<Payload>(handles[..Count]);

        for (var index = 0; index < Count; index++)
        {
            referenceSpan[index] = _payloadArray[index];
        }

        var sum = 0;
        for (var index = 0; index < Count; index++)
        {
            sum += referenceSpan[index]!.Value;
        }

        return sum;
    }
}