using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class WillDelayIntervalProperty : FourByteIntegerValue
    {
        public WillDelayIntervalProperty(uint value) 
            : base((byte)PropertyID.WillDelayInterval, value)
        {
        }
    }
}
