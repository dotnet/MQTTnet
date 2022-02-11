// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V3
{
    public sealed class MqttV311PacketFormatter : MqttV310PacketFormatter
    {
        public MqttV311PacketFormatter(MqttBufferWriter bufferWriter)
            : base(bufferWriter)
        {
        }

        protected override byte EncodeConnectPacket(MqttConnectPacket packet, MqttBufferWriter bufferWriter)
        {
            ValidateConnectPacket(packet);

            bufferWriter.WriteString("MQTT");
            bufferWriter.WriteByte(4); // 3.1.2.2 Protocol Level 4

            byte connectFlags = 0x0;
            if (packet.CleanSession)
            {
                connectFlags |= 0x2;
            }

            if (packet.WillFlag)
            {
                connectFlags |= 0x4;
                connectFlags |= (byte)((byte)packet.WillQoS << 3);

                if (packet.WillRetain)
                {
                    connectFlags |= 0x20;
                }
            }

            if (packet.Password != null && packet.Username == null)
            {
                throw new MqttProtocolViolationException("If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].");
            }

            if (packet.Password != null)
            {
                connectFlags |= 0x40;
            }

            if (packet.Username != null)
            {
                connectFlags |= 0x80;
            }

            bufferWriter.WriteByte(connectFlags);
            bufferWriter.WriteTwoByteInteger(packet.KeepAlivePeriod);
            bufferWriter.WriteString(packet.ClientId);

            if (packet.WillFlag)
            {
                bufferWriter.WriteString(packet.WillTopic);
                bufferWriter.WriteBinaryData(packet.WillMessage);
            }

            if (packet.Username != null)
            {
                bufferWriter.WriteString(packet.Username);
            }

            if (packet.Password != null)
            {
                bufferWriter.WriteBinaryData(packet.Password);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        protected override byte EncodeConnAckPacket(MqttConnAckPacket packet, MqttBufferWriter bufferWriter)
        {
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            bufferWriter.WriteByte(connectAcknowledgeFlags);
            bufferWriter.WriteByte((byte)packet.ReturnCode);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        protected override MqttBasePacket DecodeConnAckPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);
            
            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = _bufferReader.ReadByte();

            packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            packet.ReturnCode = (MqttConnectReturnCode)_bufferReader.ReadByte();

            return packet;
        }
    }
}
