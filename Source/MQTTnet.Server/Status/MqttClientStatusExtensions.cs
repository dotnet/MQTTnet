// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server;

public static class MqttClientStatusExtensions
{
    static readonly MqttServerClientDisconnectOptions DefaultDisconnectOptions = new()
    {
        ReasonCode = MqttDisconnectReasonCode.NormalDisconnection,
        ReasonString = null,
        UserProperties = null,
        ServerReference = null
    };

    public static Task DisconnectAsync(this MqttClientStatus clientStatus)
    {
        ArgumentNullException.ThrowIfNull(clientStatus);

        return clientStatus.DisconnectAsync(DefaultDisconnectOptions);
    }
}