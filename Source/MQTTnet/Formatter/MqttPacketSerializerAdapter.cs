using System;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter.V310;
using MQTTnet.Formatter.V311;
using MQTTnet.Formatter.V500;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
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

        public IMqttPacketFormatter Formatter { get; private set; }

        public ArraySegment<byte> Encode(MqttBasePacket packet)
        {
            if (Formatter == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }

            return Formatter.Encode(packet);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (Formatter == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }

            return Formatter.Decode(receivedMqttPacket);
        }

        public void FreeBuffer()
        {
            Formatter?.FreeBuffer();
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
                        Formatter = new MqttV500PacketFormatter();
                        break;
                    }

                case MqttProtocolVersion.V311:
                    {
                        Formatter = new MqttV311PacketFormatter();
                        break;
                    }

                case MqttProtocolVersion.V310:
                    {

                        Formatter = new MqttV310PacketFormatter();
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
