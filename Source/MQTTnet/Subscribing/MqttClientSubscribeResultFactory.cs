// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet;

public sealed class MqttClientSubscribeResultFactory
{
    static readonly IReadOnlyCollection<MqttUserProperty> EmptyUserProperties = new List<MqttUserProperty>();

    public MqttClientSubscribeResult Create(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
    {
        if (subscribePacket == null)
        {
            throw new ArgumentNullException(nameof(subscribePacket));
        }

        if (subAckPacket == null)
        {
            throw new ArgumentNullException(nameof(subAckPacket));
        }

        // MQTTv5.0.0 handling.
        if (subAckPacket.ReasonCodes.Any() && subAckPacket.ReasonCodes.Count != subscribePacket.TopicFilters.Count)
        {
            throw new MqttProtocolViolationException("The reason codes are not matching the topic filters [MQTT-3.9.3-1].");
        }

        var items = new List<MqttClientSubscribeResultItem>();
        for (var i = 0; i < subscribePacket.TopicFilters.Count; i++)
        {
            items.Add(CreateSubscribeResultItem(i, subscribePacket, subAckPacket));
        }

        return new MqttClientSubscribeResult(subAckPacket.PacketIdentifier, items, subAckPacket.ReasonString, subAckPacket.UserProperties ?? EmptyUserProperties);
    }

    static MqttClientSubscribeResultItem CreateSubscribeResultItem(int index, MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
    {
        var resultCode = (MqttClientSubscribeResultCode)subAckPacket.ReasonCodes[index];
        return new MqttClientSubscribeResultItem(subscribePacket.TopicFilters[index], resultCode);
    }
}