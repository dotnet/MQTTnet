using System;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public sealed class MqttApplicationMessageId: MqttApplicationMessage
    {
        public MqttApplicationMessageId():base()
        {
        }

        public Guid Id { get; set; }
    }
}
