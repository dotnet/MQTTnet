// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System.Buffers;

namespace MQTTnet.Server.Internal.Formatter;

public static class MqttPublishPacketFactory
{
    public static MqttPublishPacket Create(MqttConnectPacket connectPacket)
    {
        if (connectPacket == null)
        {
            throw new ArgumentNullException(nameof(connectPacket));
        }

        if (!connectPacket.WillFlag)
        {
            throw new MqttProtocolViolationException("The CONNECT packet contains no will message (WillFlag).");
        }

        ArraySegment<byte> willMessageBuffer = default;
        if (connectPacket.WillMessage?.Length > 0)
        {
            willMessageBuffer = new ArraySegment<byte>(connectPacket.WillMessage);
        }

        var packet = new MqttPublishPacket
        {
            Topic = connectPacket.WillTopic,
            Payload = new ReadOnlySequence<byte>(willMessageBuffer),
            QualityOfServiceLevel = connectPacket.WillQoS,
            Retain = connectPacket.WillRetain,
            ContentType = connectPacket.WillContentType,
            CorrelationData = connectPacket.WillCorrelationData,
            MessageExpiryInterval = connectPacket.WillMessageExpiryInterval,
            PayloadFormatIndicator = connectPacket.WillPayloadFormatIndicator,
            ResponseTopic = connectPacket.WillResponseTopic,
            UserProperties = connectPacket.WillUserProperties
        };

        return packet;
    }

    public static MqttPublishPacket Create(MqttApplicationMessage applicationMessage)
    {
        if (applicationMessage == null)
        {
            throw new ArgumentNullException(nameof(applicationMessage));
        }

        // Copy all values to their matching counterparts.
        // The not supported values in MQTT 3.1.1 are not serialized (excluded) later.
        var packet = new MqttPublishPacket
        {
            Topic = applicationMessage.Topic,
            Payload = applicationMessage.Payload,
            PayloadOwner = applicationMessage.PayloadOwner,
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

    public static MqttPublishPacket Create(MqttRetainedMessageMatch retainedMessage)
    {
        if (retainedMessage == null)
        {
            throw new ArgumentNullException(nameof(retainedMessage));
        }

        var publishPacket = Create(retainedMessage.ApplicationMessage);
        publishPacket.QualityOfServiceLevel = retainedMessage.SubscriptionQualityOfServiceLevel;
        return publishPacket;
    }
}