using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Benchmarks;
public sealed class ColdVsHotConfig : ManualConfig
{
    public ColdVsHotConfig()
    {
        AddJob(Job
               .Dry
               .WithId("Cold")
               .WithRuntime(CoreRuntime.Core80)
               .WithStrategy(RunStrategy.ColdStart)
               .WithWarmupCount(0)
               .WithIterationCount(1));

        AddJob(Job
               .Default
               .WithId("Hot")
               .WithBaseline(true));

        AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
    }
}