// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class SendSubscribeUnsubscribeResult
    {
        public SendSubscribeUnsubscribeResult(List<MqttClientSubscribeResult> subscribeResults, List<MqttClientUnsubscribeResult> unsubscribeResults)
        {
            SubscribeResults = subscribeResults ?? throw new ArgumentNullException(nameof(subscribeResults));
            UnsubscribeResults = unsubscribeResults ?? throw new ArgumentNullException(nameof(unsubscribeResults));
        }
        
        public List<MqttClientSubscribeResult> SubscribeResults { get; private set; }
        
        public List<MqttClientUnsubscribeResult> UnsubscribeResults { get; private set; }
    }
}