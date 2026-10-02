// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet;

public sealed class MqttClientDisconnectOptions
{
    uint _sessionExpiryInterval;

    /// <summary>
    ///     Whether Session Expiry Interval is present. Setting the interval sets this flag, including for zero.
    ///     Set this to false to omit the property and retain the CONNECT interval.
    /// </summary>
    public bool HasSessionExpiryInterval { get; set; }

    /// <summary>
    ///     Gets or sets the reason code.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientDisconnectOptionsReason Reason { get; set; } = MqttClientDisconnectOptionsReason.NormalDisconnection;

    /// <summary>
    ///     Gets or sets the reason string.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public string ReasonString { get; set; }

    /// <summary>
    ///     Gets or sets the session expiry interval.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
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
    ///     Gets or sets the user properties.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; set; }
}