using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public class BinaryDataProperty : IProperty
    {
        public BinaryDataProperty(byte id, ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Id = id;
            Data = data;
        }

        public byte Id { get; }

        public ArraySegment<byte> Data { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.Write((ushort)Data.Count);
            writer.Write(Data);
        }
    }
}
