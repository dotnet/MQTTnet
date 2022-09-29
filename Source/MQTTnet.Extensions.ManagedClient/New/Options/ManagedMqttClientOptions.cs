// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttClientOptions
    {
        public ManagedMqttClientOptions()
        {
        }

        public ManagedMqttClientOptions(MqttClientOptions clientOptions)
        {
            ClientOptions = clientOptions;
        }
        
        public MqttClientOptions ClientOptions { get; set; }

        public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IManagedMqttClientStorage Storage { get; set; }

        public int MaxPendingMessages { get; set; } = int.MaxValue;

        public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropNewMessage;

        /// <summary>
        /// Defines the maximum amount of topic filters which will be sent in a SUBSCRIBE/UNSUBSCRIBE packet.
        /// Amazon AWS limits this number to 8. The default is int.MaxValue.
        /// </summary>
        public int MaxTopicFiltersInSubscribeUnsubscribePackets { get; set; } = int.MaxValue;
    }
}
