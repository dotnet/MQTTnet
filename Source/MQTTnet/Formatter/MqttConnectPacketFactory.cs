// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnectPacketFactory
    {
        public MqttConnectPacket Create(MqttClientOptions clientOptions)
        {
            if (clientOptions == null) throw new ArgumentNullException(nameof(clientOptions));
            
            var connectPacket = new MqttConnectPacket
            {
                ClientId = clientOptions.ClientId,
                Username = clientOptions.Credentials?.Username,
                Password = clientOptions.Credentials?.Password,
                CleanSession = clientOptions.CleanSession,
                KeepAlivePeriod = (ushort) clientOptions.KeepAlivePeriod.TotalSeconds,
                AuthenticationMethod = clientOptions.AuthenticationMethod,
                AuthenticationData = clientOptions.AuthenticationData,
                WillDelayInterval = clientOptions.WillDelayInterval,
                MaximumPacketSize = clientOptions.MaximumPacketSize,
                ReceiveMaximum = clientOptions.ReceiveMaximum,
                RequestProblemInformation = clientOptions.RequestProblemInformation,
                RequestResponseInformation = clientOptions.RequestResponseInformation,
                SessionExpiryInterval = clientOptions.SessionExpiryInterval,
                TopicAliasMaximum = clientOptions.TopicAliasMaximum,
                UserProperties = clientOptions.UserProperties
            };

            if (!string.IsNullOrEmpty(clientOptions.WillTopic))
            {
                connectPacket.WillFlag = true;
                connectPacket.WillTopic = clientOptions.WillTopic;
                connectPacket.WillQoS = clientOptions.WillQualityOfServiceLevel;
                connectPacket.WillMessage = clientOptions.WillPayload;
                connectPacket.WillRetain = clientOptions.WillRetain;
                connectPacket.WillDelayInterval = clientOptions.WillDelayInterval;
                connectPacket.WillContentType = clientOptions.WillContentType;
                connectPacket.WillCorrelationData = clientOptions.WillCorrelationData;
                connectPacket.WillResponseTopic = clientOptions.WillResponseTopic;
                connectPacket.WillMessageExpiryInterval = clientOptions.WillMessageExpiryInterval;
                connectPacket.WillPayloadFormatIndicator = clientOptions.WillPayloadFormatIndicator;
                connectPacket.WillUserProperties = clientOptions.WillUserProperties;
            }

            return connectPacket;
        }
    }
}