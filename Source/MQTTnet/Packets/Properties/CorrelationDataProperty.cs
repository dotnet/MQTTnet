using System;
using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class CorrelationDataProperty : BinaryDataProperty
    {
        public CorrelationDataProperty(ArraySegment<byte> data) 
            : base((byte)MqttMessagePropertyID.CorrelationData, data)
        {
        }
    }
}
