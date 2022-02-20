// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Protocol
{
    public enum MqttPropertyId
    {
        None = 0,
        
        PayloadFormatIndicator = 1,
        MessageExpiryInterval = 2,
        ContentType = 3,
        ResponseTopic = 8,
        CorrelationData = 9,
        SubscriptionIdentifier = 11,
        SessionExpiryInterval = 17,
        AssignedClientIdentifier = 18,
        ServerKeepAlive = 19,
        AuthenticationMethod = 21,
        AuthenticationData = 22,
        RequestProblemInformation = 23,
        WillDelayInterval = 24,
        RequestResponseInformation = 25,
        ResponseInformation = 26,
        ServerReference = 28,
        ReasonString = 31,
        ReceiveMaximum = 33,
        TopicAliasMaximum = 34,
        TopicAlias = 35,
        MaximumQoS = 36,
        RetainAvailable = 37,
        UserProperty = 38,
        MaximumPacketSize = 39,
        WildcardSubscriptionAvailable = 40,
        SubscriptionIdentifiersAvailable = 41,
        SharedSubscriptionAvailable = 42
    }
}
