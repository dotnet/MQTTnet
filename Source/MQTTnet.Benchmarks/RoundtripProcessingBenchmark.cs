using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter, RankColumn]
[MemoryDiagnoser]
public class RoundtripProcessingBenchmark : BaseBenchmark
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        TestEnvironment.EnableLogger = false;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
    }

    [Benchmark]
    public void Handle_100_000_Messages_In_Receiving_Client()
    {
        //new Load_Tests().Handle_100_000_Messages_In_Receiving_Client().GetAwaiter().GetResult();
    }
}