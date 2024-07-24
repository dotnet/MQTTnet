using System;
using System.Threading;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    public sealed class MqttPacketSerializationHelper : IDisposable
    {
        readonly IMqttPacketFormatter _packetFormatter;
        readonly MqttProtocolVersion _protocolVersion;

        public MqttPacketSerializationHelper(MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311, MqttBufferWriter bufferWriter = null)
        {
            _protocolVersion = protocolVersion;

            if (bufferWriter == null)
            {
                bufferWriter = new MqttBufferWriter(4096, 65535);
            }

            _packetFormatter = MqttPacketFormatterAdapter.GetMqttPacketFormatter(_protocolVersion, bufferWriter);
        }

        public MqttPacket Decode(MqttPacketBuffer buffer)
        {
            using (var channel = new MemoryMqttChannel(buffer.ToArray()))
            {
                var formatterAdapter = new MqttPacketFormatterAdapter(_protocolVersion, new MqttBufferWriter(4096, 65535));

                var adapter = new MqttChannelAdapter(channel, formatterAdapter, MqttNetNullLogger.Instance);
                return adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
        }

        public MqttPacketBuffer Encode(MqttPacket packet)
        {
            return _packetFormatter.Encode(packet);
        }

        public static TPacket EncodeAndDecodePacket<TPacket>(TPacket packet, MqttProtocolVersion protocolVersion) where TPacket : MqttPacket
        {
            using (var helper = new MqttPacketSerializationHelper(protocolVersion))
            {
                var buffer = helper.Encode(packet);
                return (TPacket)helper.Decode(buffer);
            }
        }

        public static byte[] EncodePacket(MqttPacket packet)
        {
            using (var helper = new MqttPacketSerializationHelper())
            {
                return helper.Encode(packet).ToArray();
            }
        }
    }
}