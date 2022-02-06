// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttUnsubscribePacketFactory
    {
        public MqttUnsubscribePacket Create(MqttClientUnsubscribeOptions clientUnsubscribeOptions)
        {
            if (clientUnsubscribeOptions == null) throw new ArgumentNullException(nameof(clientUnsubscribeOptions));

            var packet = new MqttUnsubscribePacket();

            if (clientUnsubscribeOptions.TopicFilters != null)
            {
                packet.TopicFilters.AddRange(clientUnsubscribeOptions.TopicFilters);
            }

            if (clientUnsubscribeOptions.UserProperties != null)
            {
                packet.Properties.UserProperties.AddRange(clientUnsubscribeOptions.UserProperties);
            }

            return packet;
        }
    }
}