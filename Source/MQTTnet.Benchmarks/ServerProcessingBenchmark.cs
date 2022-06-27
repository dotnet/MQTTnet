using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Tests.Mockups;
using MQTTnet.Tests.Server;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class ServerProcessingBenchmark
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