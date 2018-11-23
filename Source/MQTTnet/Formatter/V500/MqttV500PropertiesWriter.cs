using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V500
{
    public class MqttV500PropertiesWriter
    {
        private readonly MqttPacketWriter _packetWriter = new MqttPacketWriter();

        public void Write(MqttPropertyID id, bool? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.Write(value.Value ? (byte)0x1 : (byte)0x0);
        }

        public void Write(MqttPropertyID id, ushort? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.Write(value.Value);
        }

        public void WriteAsVariableLengthInteger(MqttPropertyID id, uint? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.WriteVariableLengthInteger(value.Value);
        }

        public void WriteAsFourByteInteger(MqttPropertyID id, uint? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.Write((byte)(value.Value >> 24));
            _packetWriter.Write((byte)(value.Value >> 16));
            _packetWriter.Write((byte)(value.Value >> 8));
            _packetWriter.Write((byte)value.Value);
        }

        public void Write(MqttPropertyID id, string value)
        {
            if (value == null)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.WriteWithLengthPrefix(value);
        }

        public void Write(MqttPropertyID id, byte[] value)
        {
            if (value == null)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.WriteWithLengthPrefix(value);
        }

        public void WriteUserProperties(List<MqttUserProperty> userProperties)
        {
            if (userProperties == null || userProperties.Count == 0)
            {
                return;
            }

            var propertyWriter = new MqttPacketWriter();
            foreach (var property in userProperties)
            {
                propertyWriter.WriteWithLengthPrefix(property.Name);
                propertyWriter.WriteWithLengthPrefix(property.Value);
            }

            _packetWriter.Write((byte)MqttPropertyID.UserProperty);
            _packetWriter.WriteVariableLengthInteger((uint)propertyWriter.Length);
            _packetWriter.Write(propertyWriter);
        }

        public void WriteToPacket(MqttPacketWriter packetWriter)
        {
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            packetWriter.WriteVariableLengthInteger((uint)_packetWriter.Length);
            packetWriter.Write(packetWriter);
        }
    }
}
