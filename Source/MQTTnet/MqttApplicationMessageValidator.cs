// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public static class MqttApplicationMessageValidator
    {
        public static void ThrowIfNotSupported(MqttApplicationMessage applicationMessage, MqttProtocolVersion protocolVersion)
        {
            ArgumentNullException.ThrowIfNull(applicationMessage);

            if (protocolVersion == MqttProtocolVersion.V500)
            {
                // Everything is supported.
                return;
            }

            if (applicationMessage.ContentType?.Any() == true)
            {
                Throw(nameof(applicationMessage.ContentType));
            }

            if (applicationMessage.UserProperties?.Any() == true)
            {
                Throw(nameof(applicationMessage.UserProperties));
            }

            if (applicationMessage.CorrelationData.Length > 0)
            {
                Throw(nameof(applicationMessage.CorrelationData));
            }

            if (applicationMessage.ResponseTopic?.Any() == true)
            {
                Throw(nameof(applicationMessage.ResponseTopic));
            }

            if (applicationMessage.SubscriptionIdentifiers?.Any() == true)
            {
                Throw(nameof(applicationMessage.SubscriptionIdentifiers));
            }

            if (applicationMessage.TopicAlias > 0)
            {
                Throw(nameof(applicationMessage.TopicAlias));
            }

            if (applicationMessage.PayloadFormatIndicator != MqttPayloadFormatIndicator.Unspecified)
            {
                Throw(nameof(applicationMessage.PayloadFormatIndicator));
            }
        }

        static void Throw(string featureName)
        {
            throw new NotSupportedException($"Feature {featureName} requires MQTT version 5.0.0.");
        }
    }
}