// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Formatter;

public static class MqttPublishPacketFactory
{
    public static MqttPublishPacket Create(MqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        // Copy all values to their matching counterparts.
        // The not supported values in MQTT 3.1.1 are not serialized (excluded) later.
        var packet = new MqttPublishPacket
        {
            Topic = applicationMessage.Topic,
            Payload = applicationMessage.Payload,
            QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
            Retain = applicationMessage.Retain,
            Dup = applicationMessage.Dup,
            ContentType = applicationMessage.ContentType,
            CorrelationData = applicationMessage.CorrelationData,
            MessageExpiryInterval = applicationMessage.MessageExpiryInterval,
            PayloadFormatIndicator = applicationMessage.PayloadFormatIndicator,
            ResponseTopic = applicationMessage.ResponseTopic,
            TopicAlias = applicationMessage.TopicAlias,
            SubscriptionIdentifiers = applicationMessage.SubscriptionIdentifiers,
            UserProperties = applicationMessage.UserProperties
        };

        return packet;
    }
}