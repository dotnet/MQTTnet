// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttSubscription
    {
        public ManagedMqttSubscription(MqttClientSubscribeOptions subscribeOptions)
        {
            SubscribeOptions = subscribeOptions ?? throw new ArgumentNullException(nameof(subscribeOptions));

            Topic = subscribeOptions.TopicFilters.First().Topic;
        }

        public bool IsObsolete => UnsubscribeOptions != null;

        public bool IsPending { get; set; } = true;

        public MqttClientSubscribeOptions SubscribeOptions { get; }

        public string Topic { get; }

        public MqttClientUnsubscribeOptions UnsubscribeOptions { get; private set; }

        public void MarkAsUnsubscribed(MqttClientUnsubscribeOptions options)
        {
            UnsubscribeOptions = options ?? throw new ArgumentNullException(nameof(options));
        }
    }
}