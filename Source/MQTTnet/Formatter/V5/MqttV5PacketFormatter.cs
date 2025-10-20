// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter.V5;

public sealed class MqttV5PacketFormatter : IMqttPacketFormatter
{
    readonly MqttV5PacketDecoder _decoder;
    readonly MqttV5PacketEncoder _encoder;

    public MqttV5PacketFormatter(MqttBufferWriter bufferWriter)
    {
        _decoder = new MqttV5PacketDecoder();
        _encoder = new MqttV5PacketEncoder(bufferWriter);
    }

    public MqttPacket? Decode(ReceivedMqttPacket receivedPacket)
    {
        return _decoder.Decode(receivedPacket);
    }

    public MqttPacketBuffer Encode(MqttPacket packet)
    {
        return _encoder.Encode(packet);
    }
}