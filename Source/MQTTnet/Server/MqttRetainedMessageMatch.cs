// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttRetainedMessageMatch
    {
        public MqttApplicationMessage ApplicationMessage { get; set; }

        public MqttQualityOfServiceLevel SubscriptionQualityOfServiceLevel { get; set; }
    }
}