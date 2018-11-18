using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class MessageExpiryIntervalProperty : FourByteIntegerValue
    {
        public MessageExpiryInterval(uint value) 
            : base((byte)PropertyID.MessageExpiryInterval, value)
        {
        }
    }
}
