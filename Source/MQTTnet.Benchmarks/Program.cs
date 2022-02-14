// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Running;
using MQTTnet.Benchmarks.Configurations;
using MQTTnet.Diagnostics;

namespace MQTTnet.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"MQTTnet - Benchmarks ({TargetFrameworkProvider.TargetFramework})");
            Console.WriteLine("--------------------------------------------------------");
            Console.WriteLine("1 = MessageProcessingBenchmark");
            Console.WriteLine("2 = SerializerBenchmark");
            Console.WriteLine("3 = LoggerBenchmark");
            Console.WriteLine("4 = TopicFilterComparerBenchmark");
            Console.WriteLine("5 = ChannelAdapterBenchmark");
            Console.WriteLine("6 = MqttTcpChannelBenchmark");
            Console.WriteLine("7 = TcpPipesBenchmark");
            Console.WriteLine("8 = MessageProcessingMqttConnectionContextBenchmark");
            Console.WriteLine("9 = ServerProcessingBenchmark");
            Console.WriteLine("a = MqttPacketReaderWriterBenchmark");
            Console.WriteLine("b = RoundtripBenchmark");
            Console.WriteLine("c = SubscribeBenchmark");
            Console.WriteLine("d = UnsubscribeBenchmark");
            Console.WriteLine("e = MessageDeliveryBenchmark");

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
                case '5':
                    BenchmarkRunner.Run<ChannelAdapterBenchmark>();
                    break;
                case '6':
                    BenchmarkRunner.Run<MqttTcpChannelBenchmark>();
                    break;
                case '7':
                    BenchmarkRunner.Run<TcpPipesBenchmark>();
                    break;
                case '8':
                    BenchmarkRunner.Run<MessageProcessingMqttConnectionContextBenchmark>(new RuntimeCompareConfig());
                    break;
                case '9':
                    BenchmarkRunner.Run<ServerProcessingBenchmark>();
                    break;
                case 'a':
                    BenchmarkRunner.Run(typeof(MqttPacketReaderWriterBenchmark));
                    break;
                case 'b':
                    BenchmarkRunner.Run<RoundtripProcessingBenchmark>();
                    break;
                case 'c':
                    BenchmarkRunner.Run<SubscribeBenchmark>();
                    break;
                case 'd':
                    BenchmarkRunner.Run<UnsubscribeBenchmark>();
                    break;
                case 'e':
                    BenchmarkRunner.Run<MessageDeliveryBenchmark>();
                    break;
            }

            Console.ReadLine();
        }
    }
}
