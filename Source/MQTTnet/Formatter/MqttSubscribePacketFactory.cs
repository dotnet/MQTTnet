// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Formatter;

public static class MqttSubscribePacketFactory
{
    public static MqttSubscribePacket Create(MqttClientSubscribeOptions clientSubscribeOptions)
    {
        ArgumentNullException.ThrowIfNull(clientSubscribeOptions);

        var packet = new MqttSubscribePacket
        {
            TopicFilters = clientSubscribeOptions.TopicFilters,
            SubscriptionIdentifier = clientSubscribeOptions.SubscriptionIdentifier,
            UserProperties = clientSubscribeOptions.UserProperties
        };

        return packet;
    }
}