using System;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter.V3;
using MQTTnet.Formatter.V5;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public class MqttPacketFormatterAdapter
    {
        private IMqttPacketFormatter _formatter;

        public MqttPacketFormatterAdapter()
        {
        }

        public MqttPacketFormatterAdapter(MqttProtocolVersion protocolVersion)
        {
            UseProtocolVersion(protocolVersion);
        }

        public MqttProtocolVersion ProtocolVersion { get; private set; }

        public IMqttDataConverter DataConverter
        {
            get
            {
                ThrowIfFormatterNotSet();
                return _formatter.DataConverter;
            }
        }

        public ArraySegment<byte> Encode(MqttBasePacket packet)
        {
            ThrowIfFormatterNotSet();

            return _formatter.Encode(packet);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            ThrowIfFormatterNotSet();

            return _formatter.Decode(receivedMqttPacket);
        }

        public void FreeBuffer()
        {
            _formatter?.FreeBuffer();
        }

        public void DetectProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
        {
            var protocolVersion = ParseProtocolVersion(receivedMqttPacket);

            // Reset the position of the stream because the protocol version is part of 
            // the regular CONNECT packet. So it will not properly deserialized if this
            // data is missing.
            receivedMqttPacket.Body.Seek(0);

            UseProtocolVersion(protocolVersion);
        }

        private void UseProtocolVersion(MqttProtocolVersion protocolVersion)
        {
            if (protocolVersion == MqttProtocolVersion.Unknown)
            {
                throw new InvalidOperationException("MQTT protocol version is invalid.");
            }

            ProtocolVersion = protocolVersion;

            switch (protocolVersion)
            {
                case MqttProtocolVersion.V500:
                    {
                        _formatter = new MqttV500PacketFormatter();
                        break;
                    }

                case MqttProtocolVersion.V311:
                    {
                        _formatter = new MqttV311PacketFormatter();
                        break;
                    }

                case MqttProtocolVersion.V310:
                    {

                        _formatter = new MqttV310PacketFormatter();
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

        private void ThrowIfFormatterNotSet()
        {
            if (_formatter == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }
        }
    }
}
