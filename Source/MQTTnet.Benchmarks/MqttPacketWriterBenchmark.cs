using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [RPlotExporter, RankColumn]
    [MemoryDiagnoser]
    public class MqttPacketWriterBenchmark
    {
        readonly byte[] _payload = new byte[1024];
        
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
        public void MqttPacketWriter()
        {
            Execute(new MqttPacketWriter());
        }
        
        [Benchmark]
        public void SpanBasedMqttPacketWriter()
        {
            Execute(new SpanBasedMqttPacketWriter());
        }

        void Execute(IMqttPacketWriter packetWriter)
        {
            for (var i = 0; i < 100000; i++)
            {
                packetWriter.WriteWithLengthPrefix("A relative short string.");
                packetWriter.Write(0x01);
                packetWriter.Write(0x02);
                packetWriter.WriteVariableLengthInteger(5647382);
                packetWriter.WriteWithLengthPrefix("A relative short string.");
                packetWriter.WriteVariableLengthInteger(857457489);
                packetWriter.WriteWithLengthPrefix(_payload);
                packetWriter.Write(2);
                packetWriter.Write(0x02);
                packetWriter.WriteWithLengthPrefix("fjgffiogfhgfhoihgoireghreghreguhreguireoghreouighreouighreughreguiorehreuiohruiorehreuioghreug");
                packetWriter.WriteWithLengthPrefix(_payload);
                
                packetWriter.Reset(0);
            }
        }
    }
}