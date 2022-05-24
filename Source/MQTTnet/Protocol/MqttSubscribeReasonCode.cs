// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Protocol
{
    public enum MqttSubscribeReasonCode
    {
        // Compatible with MQTTv3.1.1.
        GrantedQoS0 = 0x00,
        GrantedQoS1 = 0x01,
        GrantedQoS2 = 0x02,
        UnspecifiedError = 0x80,
        
        // New in MQTTv5.
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        TopicFilterInvalid = 143,
        PacketIdentifierInUse = 145,
        QuotaExceeded = 151,
        SharedSubscriptionsNotSupported = 158,
        SubscriptionIdentifiersNotSupported = 161,
        WildcardSubscriptionsNotSupported = 162
    }
}
