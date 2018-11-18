using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class AuthenticationMethodProperty : StringProperty
    {
        public AuthenticationMethodProperty(string value) 
            : base((byte)MqttMessagePropertyID.AuthenticationMethod, value)
        {
        }
    }
}
