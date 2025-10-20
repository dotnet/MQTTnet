// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttDisconnectPacket : MqttPacket
{
    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string? ServerReference { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public uint SessionExpiryInterval { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public List<MqttUserProperty>? UserProperties { get; set; }

    public override string ToString()
    {
        return $"Disconnect: [ReasonCode={ReasonCode}] [ReasonString={ReasonString}] [ServerReference={ServerReference}] [SessionExpiryInterval={SessionExpiryInterval}]";
    }
}