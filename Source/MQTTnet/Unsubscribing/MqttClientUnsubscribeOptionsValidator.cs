// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;

namespace MQTTnet;

public static class MqttClientUnsubscribeOptionsValidator
{
    public static void ThrowIfNotSupported(MqttClientUnsubscribeOptions options, MqttProtocolVersion protocolVersion)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (protocolVersion == MqttProtocolVersion.V500)
        {
            // Everything is supported.
            return;
        }

        if (options.UserProperties?.Count > 0)
        {
            Throw(nameof(options.UserProperties));
        }
    }

    static void Throw(string featureName)
    {
        throw new NotSupportedException($"Feature {featureName} requires MQTT version 5.0.0.");
    }
}