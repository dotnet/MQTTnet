using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Buffers;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttPacketWriter_Tests
    {
        [TestMethod]
        public void Reset_After_Usage()
        {
            var writer = new MqttBufferWriter(4096, 65535);
            writer.WriteString("AString");
            writer.WriteByte(0x1);
            writer.WriteByte(0x0);
            writer.WriteByte(0x1);
            writer.WriteVariableByteInteger(1234U);
            writer.WriteVariableByteInteger(9876U);

            writer.Reset(0);

            Assert.AreEqual(0, writer.Length);
        }

        [TestMethod]
        public void Use_All_Data_Types()
        {
            var writer = new MqttBufferWriter(4096, 65535);
            writer.WriteString("AString");
            writer.WriteByte(0x1);
            writer.WriteByte(0x0);
            writer.WriteByte(0x1);
            writer.WriteVariableByteInteger(1234U);
            writer.WriteVariableByteInteger(9876U);

            var buffer = writer.GetBuffer();

            var reader = new MqttBufferReader();
            reader.SetBuffer(buffer, 0, writer.Length);

            Assert.AreEqual("AString", reader.ReadString());
            Assert.IsTrue(reader.ReadByte() == 1);
            Assert.IsTrue(reader.ReadByte() == 0);
            Assert.IsTrue(reader.ReadByte() == 1);
            Assert.AreEqual(1234U, reader.ReadVariableByteInteger());
            Assert.AreEqual(9876U, reader.ReadVariableByteInteger());
        }
        
        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Throw_If_String_Too_Long()
        {
            var writer = new MqttBufferWriter(4096, 65535);
            
            writer.WriteString(string.Empty.PadLeft(65536));
        }

        [TestMethod]
        public void Write_And_Read_Multiple_Times()
        {
            var writer = new MqttBufferWriter(4096, 65535);
            writer.WriteString("A relative short string.");
            writer.WriteBinary(new byte[1234]);
            writer.WriteByte(0x01);
            writer.WriteByte(0x02);
            writer.WriteVariableByteInteger(5647382);
            writer.WriteString("A relative short string.");
            writer.WriteVariableByteInteger(8574489);
            writer.WriteBinary(new byte[48]);
            writer.WriteByte(2);
            writer.WriteByte(0x02);
            writer.WriteString("fjgffiogfhgfhoihgoireghreghreguhreguireoghreouighreouighreughreguiorehreuiohruiorehreuioghreug");
            writer.WriteBinary(new byte[3]);

            var readPayload = new ArraySegment<byte>(writer.GetBuffer(), 0, writer.Length).ToArray();

            var reader = new MqttBufferReader();
            reader.SetBuffer(readPayload, 0, readPayload.Length);

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
    }
}