// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using System;
using System.Collections.Generic;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class SubscriptionsResultEventArgs : EventArgs
    {
        public SubscriptionsResultEventArgs(List<MqttClientSubscribeResult> subscribeResult, List<MqttClientUnsubscribeResult> unsubscribeResult)
        {
            SubscribeResult = subscribeResult ?? throw new ArgumentNullException(nameof(subscribeResult));
            UnsubscribeResult = unsubscribeResult ?? throw new ArgumentNullException(nameof(unsubscribeResult));
        }

        public List<MqttClientSubscribeResult> SubscribeResult { get; }

        public List<MqttClientUnsubscribeResult> UnsubscribeResult { get; }
    }
}
