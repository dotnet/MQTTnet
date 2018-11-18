using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class SubscriptionIdentifierProperty : VariableByteIntegerProperty
    {
        public SubscriptionIdentifierProperty(uint value) 
            : base((byte)PropertyID.SubscriptionIdentifier, value)
        {
        }
    }
}
