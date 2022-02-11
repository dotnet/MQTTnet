// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Protocol
{
    public enum MqttRetainHandling
    {
        SendAtSubscribe = 0,
        
        SendAtSubscribeIfNewSubscriptionOnly = 1,
        
        DoNotSendOnSubscribe = 2
    }
}
