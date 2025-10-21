// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter.V3;
using MQTTnet.Formatter.V5;
using MQTTnet.Packets;

namespace MQTTnet.Formatter;

public sealed class MqttPacketFormatterAdapter
{
    readonly MqttBufferReader _bufferReader = new();
    readonly MqttBufferWriter _bufferWriter;

    IMqttPacketFormatter _formatter;

    public MqttPacketFormatterAdapter(MqttBufferWriter mqttBufferWriter)
    {
        _bufferWriter = mqttBufferWriter ?? throw new ArgumentNullException(nameof(mqttBufferWriter));
    }

    public MqttPacketFormatterAdapter(MqttProtocolVersion protocolVersion, MqttBufferWriter bufferWriter)
        : this(bufferWriter)
    {
        UseProtocolVersion(protocolVersion);
    }

    public MqttProtocolVersion ProtocolVersion { get; private set; } = MqttProtocolVersion.Unknown;

    public void Cleanup()
    {
        _bufferWriter.Cleanup();
    }

    public MqttPacket Decode(ReceivedMqttPacket receivedMqttPacket)
    {
        ThrowIfFormatterNotSet();
        return _formatter.Decode(receivedMqttPacket);
    }

    public void DetectProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
    {
        var protocolVersion = ParseProtocolVersion(receivedMqttPacket);
        UseProtocolVersion(protocolVersion);
    }

    public MqttPacketBuffer Encode(MqttPacket packet)
    {
        ThrowIfFormatterNotSet();
        return _formatter.Encode(packet);
    }

    public static IMqttPacketFormatter GetMqttPacketFormatter(MqttProtocolVersion protocolVersion, MqttBufferWriter bufferWriter)
    {
        if (protocolVersion == MqttProtocolVersion.Unknown)
        {
            throw new InvalidOperationException("MQTT protocol version is invalid.");
        }

        return protocolVersion switch
        {
            MqttProtocolVersion.V500 => new MqttV5PacketFormatter(bufferWriter),
            MqttProtocolVersion.V310 or MqttProtocolVersion.V311 => new MqttV3PacketFormatter(bufferWriter, protocolVersion),
            _ => throw new NotSupportedException()
        };
    }

    MqttProtocolVersion ParseProtocolVersion(ReceivedMqttPacket receivedMqttPacket)
    {
        if (receivedMqttPacket.Body.Count < 7)
        {
            // 2 byte protocol name length
            // at least 4 byte protocol name
            // 1 byte protocol level
            throw new MqttProtocolViolationException("CONNECT packet must have at least 7 bytes.");
        }

        _bufferReader.SetBuffer(receivedMqttPacket.Body.Array, receivedMqttPacket.Body.Offset, receivedMqttPacket.Body.Count);

        var protocolName = _bufferReader.ReadString();
        var protocolLevel = _bufferReader.ReadByte();

        // Remove the mosquitto try_private flag (MQTT 3.1.1 Bridge).
        // This flag is accepted but not yet used.
        protocolLevel &= 0x7F;

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

    void UseProtocolVersion(MqttProtocolVersion protocolVersion)
    {
        if (protocolVersion == MqttProtocolVersion.Unknown)
        {
            throw new InvalidOperationException("MQTT protocol version is invalid.");
        }

        ProtocolVersion = protocolVersion;
        _formatter = GetMqttPacketFormatter(protocolVersion, _bufferWriter);
    }
}