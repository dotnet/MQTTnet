using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class MaximumQoSProperty : ByteProperty
    {
        public MaximumQoSProperty(byte value) 
            : base((byte)MqttMessagePropertyID.MaximumQoS, value)
        {
        }
    }
}
