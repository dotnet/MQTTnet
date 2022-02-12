// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter.V5
{
    public sealed class MqttV5PacketFormatter : IMqttPacketFormatter
    {
        readonly MqttV5PacketDecoder _decoder = new MqttV5PacketDecoder();
        readonly MqttV5PacketEncoder _encoder;

        public MqttV5PacketFormatter(MqttBufferWriter writer)
        {
            _encoder = new MqttV5PacketEncoder(writer);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            return _decoder.Decode(receivedMqttPacket);
        }

        public MqttPacketBuffer Encode(MqttBasePacket mqttPacket)
        {
            return _encoder.Encode(mqttPacket);
        }

        public void FreeBuffer()
        {
            _encoder.FreeBuffer();
        }
    }
}