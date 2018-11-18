using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class WildcardSubscriptionAvailableProperty : ByteProperty
    {
        public WildcardSubscriptionAvailableProperty(byte value)
            : base((byte)MqttMessagePropertyID.WildcardSubscriptionAvailable, value)
        {
        }
    }
}
