using System;
using System.Runtime.CompilerServices;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter.V3;
using MQTTnet.Formatter.V5;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketFormatterAdapter
    {
        IMqttPacketFormatter _formatter;
               
        public MqttPacketFormatterAdapter(MqttProtocolVersion protocolVersion)
            : this(protocolVersion, new MqttPacketWriter())
        {
        }

        public MqttPacketFormatterAdapter(MqttProtocolVersion protocolVersion, IMqttPacketWriter writer)
            : this(writer)
        {
            UseProtocolVersion(protocolVersion);
        }

        public MqttPacketFormatterAdapter(IMqttPacketWriter writer)
        {
            Writer = writer;
        }        

        public MqttProtocolVersion ProtocolVersion { get; private set; } = MqttProtocolVersion.Unknown;

        public IMqttDataConverter DataConverter
        {
            get
            {
                ThrowIfFormatterNotSet();

                return _formatter.DataConverter;
            }
        }
                
        public IMqttPacketWriter Writer { get; }

        public ArraySegment<byte> Encode(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            ThrowIfFormatterNotSet();

            return _formatter.Encode(packet);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

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

        public static IMqttPacketFormatter GetMqttPacketFormatter(MqttProtocolVersion protocolVersion, IMqttPacketWriter writer)
        {
            if (protocolVersion == MqttProtocolVersion.Unknown)
            {
                throw new InvalidOperationException("MQTT protocol version is invalid.");
            }
            
            switch (protocolVersion)
            {
                case MqttProtocolVersion.V500:
                    {
                        return new MqttV500PacketFormatter(writer);
                    }
                case MqttProtocolVersion.V311:
                    {
                        return new MqttV311PacketFormatter(writer);
                    }
                case MqttProtocolVersion.V310:
                    {
                        return new MqttV310PacketFormatter(writer);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        void UseProtocolVersion(MqttProtocolVersion protocolVersion)
        {
            if (protocolVersion == MqttProtocolVersion.Unknown)
            {
                throw new InvalidOperationException("MQTT protocol version is invalid.");
            }

            ProtocolVersion = protocolVersion;
            _formatter = GetMqttPacketFormatter(protocolVersion, Writer);
        }

        static MqttProtocolVersion ParseProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            if (receivedMqttPacket.Body.Length < 7)
            {
                // 2 byte protocol name length
                // at least 4 byte protocol name
                // 1 byte protocol level
                throw new MqttProtocolViolationException("CONNECT packet must have at least 7 bytes.");
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfFormatterNotSet()
        {
            if (_formatter == null)
            {
                throw new InvalidOperationException("Protocol version not set or detected.");
            }
        }
    }
}
