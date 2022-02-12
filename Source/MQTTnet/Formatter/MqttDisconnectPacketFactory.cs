// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttDisconnectPacketFactory
    {
        readonly MqttDisconnectPacket _normalDisconnection = new MqttDisconnectPacket
        {
            ReasonCode = MqttDisconnectReasonCode.NormalDisconnection
        };

        public MqttDisconnectPacket Create(MqttDisconnectReasonCode reasonCode)
        {
            if (reasonCode == MqttDisconnectReasonCode.NormalDisconnection)
            {
                return _normalDisconnection;
            }

            return new MqttDisconnectPacket
            {
                ReasonCode = reasonCode
            };
        }

        public MqttDisconnectPacket Create(MqttClientDisconnectOptions clientDisconnectOptions)
        {
            var packet = new MqttDisconnectPacket();

            if (clientDisconnectOptions == null)
            {
                packet.ReasonCode = MqttDisconnectReasonCode.NormalDisconnection;
            }
            else
            {
                packet.ReasonCode = (MqttDisconnectReasonCode)clientDisconnectOptions.Reason;
            }

            return packet;
        }
    }
}