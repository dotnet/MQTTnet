using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacketWillProperties
    {
        public uint WillDelayInterval { get; set; }

        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; } = MqttPayloadFormatIndicator.Unspecified;
        
        public uint MessageExpiryInterval { get; set; }
        
        public string ContentType { get; set; }
        
        public string ResponseTopic { get; set; }
        
        public byte[] CorrelationData { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
    }
}