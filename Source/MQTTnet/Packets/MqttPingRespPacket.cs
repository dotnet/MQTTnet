﻿namespace MQTTnet.Packets
{
    public sealed class MqttPingRespPacket : MqttBasePacket
    {
        // This is a minor performance improvement.
        public static readonly MqttPingRespPacket Instance = new MqttPingRespPacket();

        public override string ToString()
        {
            return "PingResp";
        }
    }
}
