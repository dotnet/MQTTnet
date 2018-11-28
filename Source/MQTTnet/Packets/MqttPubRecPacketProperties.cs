﻿using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public class MqttPubRecPacketProperties
    {
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
