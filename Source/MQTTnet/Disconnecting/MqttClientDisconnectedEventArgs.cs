// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet;

public sealed class MqttClientDisconnectedEventArgs : EventArgs
{
    public MqttClientDisconnectedEventArgs(
        bool clientWasConnected,
        MqttClientConnectResult connectResult,
        MqttClientDisconnectReason reason,
        string reasonString,
        List<MqttUserProperty> userProperties,
        Exception exception)
    {
        ClientWasConnected = clientWasConnected;
        ConnectResult = connectResult;
        Exception = exception;
        Reason = reason;
        ReasonString = reasonString;
        UserProperties = userProperties;
    }

    public bool ClientWasConnected { get; }

    /// <summary>
    ///     Gets the authentication result.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientConnectResult ConnectResult { get; }

    public Exception Exception { get; }

    /// <summary>
    ///     Gets or sets the reason.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientDisconnectReason Reason { get; }

    public string ReasonString { get; }

    public List<MqttUserProperty> UserProperties { get; }
}