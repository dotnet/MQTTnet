﻿using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttPacketHeader
    {
        public MqttControlPacketType ControlPacketType { get; set; }

        public byte FixedHeader { get; set; }

        public int BodyLength { get; set; }
    }
}
