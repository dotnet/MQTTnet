using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class SessionExpiryIntervalProperty : FourByteIntegerValue
    {
        public SessionExpiryIntervalProperty(uint value) 
            : base((byte)MqttMessagePropertyID.SessionExpiryInterval, value)
        {
        }
    }
}
