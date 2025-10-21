// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet;

public sealed class MqttClientConnectedEventArgs : EventArgs
{
    public MqttClientConnectedEventArgs(MqttClientConnectResult connectResult)
    {
        ConnectResult = connectResult ?? throw new ArgumentNullException(nameof(connectResult));
    }

    /// <summary>
    ///     Gets the authentication result.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttClientConnectResult ConnectResult { get; }
}