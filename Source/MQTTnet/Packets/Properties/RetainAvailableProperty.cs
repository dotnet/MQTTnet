using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class RetainAvailableProperty : ByteProperty
    {
        public RetainAvailableProperty(byte value) 
            : base((byte)MqttMessagePropertyID.RetainAvailable, value)
        {
        }
    }
}
