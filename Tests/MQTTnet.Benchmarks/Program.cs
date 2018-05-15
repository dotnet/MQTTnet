using System;
using BenchmarkDotNet.Running;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"MQTTnet - BenchmarkApp.{TargetFrameworkInfoProvider.TargetFramework}");
            Console.WriteLine("1 = MessageProcessingBenchmark");
            Console.WriteLine("2 = SerializerBenchmark");
            Console.WriteLine("3 = LoggerBenchmark");
            Console.WriteLine("4 = TopicFilterComparerBenchmark");

            var pressedKey = Console.ReadKey(true);
            switch (pressedKey.KeyChar)
            {
                case '1':
                    BenchmarkRunner.Run<MessageProcessingBenchmark>();
                    break;
                case '2':
                    BenchmarkRunner.Run<SerializerBenchmark>();
                    break;
                case '3':
                    BenchmarkRunner.Run<LoggerBenchmark>();
                    break;
                case '4':
                    BenchmarkRunner.Run<TopicFilterComparerBenchmark>();
                    break;
            }

            Console.ReadLine();
        }
    }
}
