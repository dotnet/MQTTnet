// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server;

public sealed class MqttRetainedMessageMatch
{
    public MqttRetainedMessageMatch(MqttApplicationMessage applicationMessage, MqttQualityOfServiceLevel subscriptionQualityOfServiceLevel)
    {
        ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
        SubscriptionQualityOfServiceLevel = subscriptionQualityOfServiceLevel;
    }

    public MqttApplicationMessage ApplicationMessage { get; }

    public MqttQualityOfServiceLevel SubscriptionQualityOfServiceLevel { get; set; }
}