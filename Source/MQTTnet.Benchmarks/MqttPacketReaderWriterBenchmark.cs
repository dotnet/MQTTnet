using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    public class MqttPacketReaderWriterBenchmark
    {
        readonly byte[] _demoPayload = new byte[1024];
        
        byte[] _readPayload;

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            TestEnvironment.EnableLogger = false;
            
            var writer = new MqttBufferWriter(4096, 65535);
            writer.WriteString("A relative short string.");
            writer.WriteBinaryData(_demoPayload);
            writer.WriteByte(0x01);
            writer.WriteByte(0x02);
            writer.WriteVariableByteInteger(5647382);
            writer.WriteString("A relative short string.");
            writer.WriteVariableByteInteger(8574489);
            writer.WriteBinaryData(_demoPayload);
            writer.WriteByte(2);
            writer.WriteByte(0x02);
            writer.WriteString("fjgffiogfhgfhoihgoireghreghreguhreguireoghreouighreouighreughreguiorehreuiohruiorehreuioghreug");
            writer.WriteBinaryData(_demoPayload);

            _readPayload = new ArraySegment<byte>(writer.GetBuffer(), 0, writer.Length).ToArray();
        }

        [Benchmark]
        public void Read_100_000_Messages()
        {
            var reader = new MqttBufferReader();
            reader.SetBuffer(_readPayload, 0, _readPayload.Length);

            for (var i = 0; i < 100000; i++)
            {
                reader.Seek(0);

                reader.ReadString();
                reader.ReadBinaryData();
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadVariableByteInteger();
                reader.ReadString();
                reader.ReadVariableByteInteger();
                reader.ReadBinaryData();
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadString();
                reader.ReadBinaryData();
            }
        }
        
        [Benchmark]
        public void Write_100_000_Messages()
        {
            var writer = new MqttBufferWriter(4096, 65535);

            for (var i = 0; i < 100000; i++)
            {
                writer.WriteString("A relative short string.");
                writer.WriteByte(0x01);
                writer.WriteByte(0x02);
                writer.WriteVariableByteInteger(5647382);
                writer.WriteString("A relative short string.");
                writer.WriteVariableByteInteger(8574589);
                writer.WriteBinaryData(_demoPayload);
                writer.WriteByte(2);
                writer.WriteByte(0x02);
                writer.WriteString("fjgffiogfhgfhoihgoireghreghreguhreguireoghreouighreouighreughreguiorehreuiohruiorehreuioghreug");
                writer.WriteBinaryData(_demoPayload);

                writer.Reset(0);
            }
        }
    }
}