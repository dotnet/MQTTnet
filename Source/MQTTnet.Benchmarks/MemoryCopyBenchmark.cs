using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;


namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class MemoryCopyBenchmark
    {
        const int max_length = 1024 * 8;
        private byte[] source;
        private byte[] target;

        [Params(64 - 1, 128 - 1, 256 - 1, 512 - 1, 1024 - 1, 2048 - 1, 5096 - 1)]
        public int Length { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            source = new byte[max_length];
            target = new byte[max_length];
        }

        [Benchmark(Baseline = true)]
        public void Array_Copy()
        {
            Array.Copy(source, 0, target, 0, Length);
        }

        [Benchmark]
        public void Memory_Copy()
        {
            MQTTnet.Buffers.MqttMemoryHelper.Copy(source, 0, target, 0, Length);
        }

    }
}