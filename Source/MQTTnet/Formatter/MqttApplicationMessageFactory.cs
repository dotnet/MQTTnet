// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public static class MqttApplicationMessageFactory
    {
        public static MqttApplicationMessage Create(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null)
            {
                throw new ArgumentNullException(nameof(publishPacket));
            }

            return new MqttApplicationMessage
            {
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = publishPacket.QualityOfServiceLevel,
                Retain = publishPacket.Retain,
                Dup = publishPacket.Dup,
                ResponseTopic = publishPacket.ResponseTopic,
                ContentType = publishPacket.ContentType,
                CorrelationData = publishPacket.CorrelationData,
                MessageExpiryInterval = publishPacket.MessageExpiryInterval,
                SubscriptionIdentifiers = publishPacket.SubscriptionIdentifiers,
                TopicAlias = publishPacket.TopicAlias,
                PayloadFormatIndicator = publishPacket.PayloadFormatIndicator,
                UserProperties = publishPacket.UserProperties
            };
        }
    }
}