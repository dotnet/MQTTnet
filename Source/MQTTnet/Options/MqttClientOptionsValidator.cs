// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet;

public static class MqttClientOptionsValidator
{
    public static void ThrowIfNotSupported(MqttClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ProtocolVersion == MqttProtocolVersion.V500)
        {
            if (options.TryPrivate)
            {
                throw new NotSupportedException("Feature TryPrivate only works with MQTT version 3.1 and 3.1.1.");
            }

            return;
        }

        if (options.WillContentType?.Length > 0)
        {
            Throw(nameof(options.WillContentType));
        }

        if (options.UserProperties?.Count > 0)
        {
            Throw(nameof(options.UserProperties));
        }

        if (options.RequestProblemInformation)
        {
            // Since this value is a boolean and true by default, validation would
            // require a nullable boolean.
            //Throw(nameof(options.RequestProblemInformation));
        }

        if (options.RequestResponseInformation)
        {
            Throw(nameof(options.RequestResponseInformation));
        }

        if (options.ReceiveMaximum > 0)
        {
            Throw(nameof(options.ReceiveMaximum));
        }

        if (options.MaximumPacketSize > 0)
        {
            Throw(nameof(options.MaximumPacketSize));
        }

        // Authentication relevant properties.

        if (options.AuthenticationData?.Length > 0)
        {
            Throw(nameof(options.AuthenticationData));
        }

        if (options.AuthenticationMethod?.Length > 0)
        {
            Throw(nameof(options.AuthenticationMethod));
        }

        // Will relevant properties.

        if (options.WillPayloadFormatIndicator != MqttPayloadFormatIndicator.Unspecified)
        {
            Throw(nameof(options.WillPayloadFormatIndicator));
        }

        if (options.WillContentType?.Length > 0)
        {
            Throw(nameof(options.WillContentType));
        }

        if (options.WillCorrelationData?.Length > 0)
        {
            Throw(nameof(options.WillCorrelationData));
        }

        if (options.WillResponseTopic?.Length > 0)
        {
            Throw(nameof(options.WillResponseTopic));
        }

        if (options.WillDelayInterval > 0)
        {
            Throw(nameof(options.WillDelayInterval));
        }

        if (options.WillMessageExpiryInterval > 0)
        {
            Throw(nameof(options.WillMessageExpiryInterval));
        }

        if (options.WillUserProperties?.Count > 0)
        {
            Throw(nameof(options.WillUserProperties));
        }

        if (options.EnhancedAuthenticationHandler != null)
        {
            Throw(nameof(options.EnhancedAuthenticationHandler));
        }
    }

    static void Throw(string featureName)
    {
        throw new NotSupportedException($"Feature {featureName} requires MQTT version 5.0.0.");
    }
}