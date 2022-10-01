// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public static class MqttClientSubscribeOptionsValidator
    {
        public static void ThrowIfNotSupported(MqttClientSubscribeOptions options, MqttProtocolVersion protocolVersion)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            if (protocolVersion == MqttProtocolVersion.V500)
            {
                // Everything is supported.
                return;
            }

            if (options.UserProperties?.Any() == true)
            {
                Throw(nameof(options.UserProperties));
            }
            
            if (options.SubscriptionIdentifier != 0)
            {
                Throw(nameof(options.SubscriptionIdentifier));
            }
            
            if (options.TopicFilters?.Any(t => t.NoLocal) == true)
            {
                Throw("NoLocal");
            }
            
            if (options.TopicFilters?.Any(t => t.RetainAsPublished) == true)
            {
                Throw("RetainAsPublished");
            }
            
            if (options.TopicFilters?.Any(t => t.RetainHandling != MqttRetainHandling.SendAtSubscribe) == true)
            {
                Throw("RetainHandling");
            }
        }
        
        static void Throw(string featureName)
        {
            throw new NotSupportedException($"Feature {featureName} requires MQTT version 5.0.0.");
        }
    }
}