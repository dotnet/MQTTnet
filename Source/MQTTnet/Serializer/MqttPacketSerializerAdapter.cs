using System;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public class MqttPacketSerializerAdapter
    {
        public MqttPacketSerializerAdapter()
        {
        }

        public MqttPacketSerializerAdapter(MqttProtocolVersion protocolVersion)
        {
            UseProtocolVersion(protocolVersion);
        }

        public MqttProtocolVersion? ProtocolVersion { get; private set; }

        public IMqttPacketSerializer Serializer { get; private set; }

        public ArraySegment<byte> Serialize(MqttBasePacket packet)
        {
            if (Serializer == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }

            return Serializer.Serialize(packet);
        }

        public MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket)
        {
            if (Serializer == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }

            return Serializer.Deserialize(receivedMqttPacket);
        }

        public void FreeBuffer()
        {
            Serializer?.FreeBuffer();
        }

        public void DetectProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
        {
            var protocolVersion = ParseProtocolVersion(receivedMqttPacket);

            // Reset the position of the stream beacuse the protocol version is part of 
            // the regular CONNECT packet. So it will not properly deserialized if this
            // data is missing.
            receivedMqttPacket.Body.Seek(0);

            UseProtocolVersion(protocolVersion);
        }

        private void UseProtocolVersion(MqttProtocolVersion protocolVersion)
        {
            ProtocolVersion = protocolVersion;

            switch (protocolVersion)
            {
                case MqttProtocolVersion.V500:
                    {
                        Serializer = new MqttV500PacketSerializer();
                        break;
                    }

                case MqttProtocolVersion.V311:
                    {
                        Serializer = new MqttV311PacketSerializer();
                        break;
                    }

                case MqttProtocolVersion.V310:
                    {

                        Serializer = new MqttV310PacketSerializer();
                        break;
                    }
                    
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private MqttProtocolVersion ParseProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            var protocolName = receivedMqttPacket.Body.ReadStringWithLengthPrefix();
            var protocolLevel = receivedMqttPacket.Body.ReadByte();

            if (protocolName == "MQTT")
            {
                if (protocolLevel == 5)
                {
                    return MqttProtocolVersion.V500;
                }

                if (protocolLevel == 4)
                {
                    return MqttProtocolVersion.V311;
                }

                throw new MqttProtocolViolationException($"Protocol level '{protocolLevel}' not supported.");
            }

            if (protocolName == "MQIsdp")
            {
                if (protocolLevel == 3)
                {
                    return MqttProtocolVersion.V310;
                }

                throw new MqttProtocolViolationException($"Protocol level '{protocolLevel}' not supported.");
            }

            throw new MqttProtocolViolationException($"Protocol '{protocolName}' not supported.");
        }
    }
}
