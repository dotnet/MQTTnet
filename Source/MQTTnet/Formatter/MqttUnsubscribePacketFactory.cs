// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Formatter;

public static class MqttUnsubscribePacketFactory
{
    public static MqttUnsubscribePacket Create(MqttClientUnsubscribeOptions clientUnsubscribeOptions)
    {
        ArgumentNullException.ThrowIfNull(clientUnsubscribeOptions);

        var packet = new MqttUnsubscribePacket
        {
            UserProperties = clientUnsubscribeOptions.UserProperties
        };

        if (clientUnsubscribeOptions.TopicFilters != null)
        {
            packet.TopicFilters.AddRange(clientUnsubscribeOptions.TopicFilters);
        }

        return packet;
    }
}