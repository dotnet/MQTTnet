using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class MaximumPacketSizeProperty : FourByteIntegerValue
    {
        public MaximumPacketSizeProperty(uint value) 
            : base((byte)MqttMessagePropertyID.MaximumPacketSize, value)
        {
        }
    }
}
