// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttDisconnectPacket : MqttPacket
{
    uint _sessionExpiryInterval;

    /// <summary>
    ///     Whether Session Expiry Interval is present. Setting the interval sets this flag, including for zero.
    ///     Set this to false to omit the property and retain the CONNECT interval.
    /// </summary>
    public bool HasSessionExpiryInterval { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string ReasonString { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string ServerReference { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public uint SessionExpiryInterval
    {
        get => _sessionExpiryInterval;
        set
        {
            _sessionExpiryInterval = value;
            HasSessionExpiryInterval = true;
        }
    }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; set; }

    public override string ToString()
    {
        return $"Disconnect: [ReasonCode={ReasonCode}] [ReasonString={ReasonString}] [ServerReference={ServerReference}] [SessionExpiryInterval={SessionExpiryInterval}]";
    }
}