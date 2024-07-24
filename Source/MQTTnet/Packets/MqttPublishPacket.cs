// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttPublishPacket : MqttPacketWithIdentifier
{
    public string ContentType { get; set; }

    public byte[] CorrelationData { get; set; }

    public bool Dup { get; set; }

    public uint MessageExpiryInterval { get; set; }

    public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; } = MqttPayloadFormatIndicator.Unspecified;

    public ArraySegment<byte> PayloadSegment { set { Payload = new ReadOnlySequence<byte>(value); } }

    public ReadOnlySequence<byte> Payload { get; set; }

    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

    public string ResponseTopic { get; set; }

    public bool Retain { get; set; }

    public List<uint> SubscriptionIdentifiers { get; set; }

    public string Topic { get; set; }

    public ushort TopicAlias { get; set; }

    public List<MqttUserProperty> UserProperties { get; set; }

    public override string ToString()
    {
        return
            $"Publish: [Topic={Topic}] [PayloadLength={Payload.Length}] [QoSLevel={QualityOfServiceLevel}] [Dup={Dup}] [Retain={Retain}] [PacketIdentifier={PacketIdentifier}]";
    }
}