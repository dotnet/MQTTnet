// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Tests.Mockups;
using MQTTnet.Tests.Server;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class ServerProcessingBenchmark : BaseBenchmark
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
        public void Handle_100_000_Messages_In_Server_MqttClient()
        {
            new Load_Tests().Handle_100_000_Messages_In_Server().GetAwaiter().GetResult();
        }
        
        //[Benchmark]
        public void Handle_100_000_Messages_In_Server_LowLevelMqttClient()
        {
            new Load_Tests().Handle_100_000_Messages_In_Low_Level_Client().GetAwaiter().GetResult();
        }
    }
}