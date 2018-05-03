using System;
using BenchmarkDotNet.Running;

namespace MQTTnet.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MessageProcessingBenchmark>();
            Console.ReadLine();
        }
    }
}
