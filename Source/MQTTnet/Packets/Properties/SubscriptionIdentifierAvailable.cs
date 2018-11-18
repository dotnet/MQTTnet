using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class SubscriptionIdentifierAvailable : ByteProperty
    {
        public SubscriptionIdentifierAvailable(byte value) 
            : base((byte)MqttMessagePropertyID.SubscriptionIdentifierAvailable, value)
        {
        }
    }
}
