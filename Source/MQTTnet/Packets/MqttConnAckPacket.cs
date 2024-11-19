// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttConnAckPacket : MqttPacket
{
    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string AssignedClientIdentifier { get; set; }

    public ReadOnlyMemory<byte> AuthenticationData { get; set; }

    public string AuthenticationMethod { get; set; }

    /// <summary>
    ///     Added in MQTTv3.1.1.
    /// </summary>
    public bool IsSessionPresent { get; set; }

    public uint MaximumPacketSize { get; set; }

    public MqttQualityOfServiceLevel MaximumQoS { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public MqttConnectReasonCode ReasonCode { get; set; }

    public string ReasonString { get; set; }

    public ushort ReceiveMaximum { get; set; }

    public string ResponseInformation { get; set; }

    public bool RetainAvailable { get; set; }

    public MqttConnectReturnCode ReturnCode { get; set; }

    public ushort ServerKeepAlive { get; set; }

    public string ServerReference { get; set; }

    public uint SessionExpiryInterval { get; set; }

    public bool SharedSubscriptionAvailable { get; set; }

    public bool SubscriptionIdentifiersAvailable { get; set; }

    public ushort TopicAliasMaximum { get; set; }

    public List<MqttUserProperty> UserProperties { get; set; }

    public bool WildcardSubscriptionAvailable { get; set; }

    public override string ToString()
    {
        return $"ConnAck: [ReturnCode={ReturnCode}] [ReasonCode={ReasonCode}] [IsSessionPresent={IsSessionPresent}]";
    }
}