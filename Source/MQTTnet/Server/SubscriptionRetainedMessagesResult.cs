// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public readonly struct SubscriptionRetainedMessagesResult
    {
        public SubscriptionRetainedMessagesResult(MqttSubscription subscription, bool isNewSubscription)
        {
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            IsNewSubscription = isNewSubscription;
            RetainedMessages = new List<MqttApplicationMessage>();
        }

        public bool IsNewSubscription { get; }

        public MqttSubscription Subscription { get; }

        public List<MqttApplicationMessage> RetainedMessages { get; }
    }
}