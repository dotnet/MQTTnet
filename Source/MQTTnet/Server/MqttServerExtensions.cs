// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public static class MqttServerExtensions
    {
        public static Task SubscribeAsync(this MqttServer server, string clientId, params MqttTopicFilter[] topicFilters)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return server.SubscribeAsync(clientId, topicFilters);
        }
        
        public static Task SubscribeAsync(this MqttServer server, string clientId, string topic)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.SubscribeAsync(clientId, new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }
    }
}
