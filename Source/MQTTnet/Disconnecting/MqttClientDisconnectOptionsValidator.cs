// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Formatter;

namespace MQTTnet;

public static class MqttClientDisconnectOptionsValidator
{
    public static void ThrowIfNotSupported(MqttClientDisconnectOptions options, MqttProtocolVersion protocolVersion)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (protocolVersion == MqttProtocolVersion.V500)
        {
            // Everything is supported.
            return;
        }

        if (options.ReasonString?.Length > 0)
        {
            Throw(nameof(options.ReasonString));
        }

        if (options.Reason != MqttClientDisconnectOptionsReason.NormalDisconnection)
        {
            Throw(nameof(options.Reason));
        }
    }

    static void Throw(string featureName)
    {
        throw new NotSupportedException($"Feature {featureName} requires MQTT version 5.0.0.");
    }
}