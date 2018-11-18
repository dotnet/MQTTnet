using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class SharedSubscriptionAvailableProperty : ByteProperty
    {
        public SharedSubscriptionAvailableProperty(byte value) 
            : base((byte)MqttMessagePropertyID.SharedSubscriptionAvailable, value)
        {
        }
    }
}
