// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Packets;

public static class MqttPacketExtensions
{
    public static string GetRfcName(this MqttPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        switch (packet)
        {
            case MqttConnectPacket _:
            {
                return "CONNECT";
            }

            case MqttConnAckPacket _:
            {
                return "CONNACK";
            }

            case MqttAuthPacket _:
            {
                return "AUTH";
            }

            case MqttDisconnectPacket _:
            {
                return "DISCONNECT";
            }

            case MqttPingReqPacket _:
            {
                return "PINGREQ";
            }

            case MqttPingRespPacket _:
            {
                return "PINGRESP";
            }

            case MqttSubscribePacket _:
            {
                return "SUBSCRIBE";
            }

            case MqttSubAckPacket _:
            {
                return "SUBACK";
            }

            case MqttUnsubscribePacket _:
            {
                return "UNSUBSCRIBE";
            }

            case MqttUnsubAckPacket _:
            {
                return "UNSUBACK";
            }

            case MqttPublishPacket _:
            {
                return "PUBLISH";
            }

            case MqttPubAckPacket _:
            {
                return "PUBACK";
            }

            case MqttPubRelPacket _:
            {
                return "PUBREL";
            }

            case MqttPubRecPacket _:
            {
                return "PUBREC";
            }

            case MqttPubCompPacket _:
            {
                return "PUBCOMP";
            }

            default:
            {
                return packet.GetType().Name;
            }
        }
    }
}