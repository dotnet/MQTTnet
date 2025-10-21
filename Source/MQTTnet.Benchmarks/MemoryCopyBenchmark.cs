using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;


namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter, RankColumn]
[MemoryDiagnoser]
public class MemoryCopyBenchmark
{
    const int MaxLength = 1024 * 8;

    byte[] _source;
    byte[] _target;

    [Params(64 - 1, 128 - 1, 256 - 1, 512 - 1, 1024 - 1, 2048 - 1, 5096 - 1)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _source = new byte[MaxLength];
        _target = new byte[MaxLength];
    }

    [Benchmark(Baseline = true)]
    public void Array_Copy()
    {
        Array.Copy(_source, 0, _target, 0, Length);
    }

    [Benchmark]
    public void Memory_Copy()
    {
        Internal.MqttMemoryHelper.Copy(_source, 0, _target, 0, Length);
    }

}