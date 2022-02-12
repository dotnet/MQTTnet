// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttSubscribePacketFactory
    {
        public MqttSubscribePacket Create(MqttClientSubscribeOptions clientSubscribeOptions)
        {
            if (clientSubscribeOptions == null)
            {
                throw new ArgumentNullException(nameof(clientSubscribeOptions));
            }

            var packet = new MqttSubscribePacket
            {
                TopicFilters = clientSubscribeOptions.TopicFilters,
                SubscriptionIdentifier = clientSubscribeOptions.SubscriptionIdentifier,
                UserProperties = clientSubscribeOptions.UserProperties
            };

            return packet;
        }
    }
}