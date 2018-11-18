namespace MQTTnet.Packets.Properties
{
    public class AuthenticationMethodProperty : StringProperty
    {
        public AuthenticationMethodProperty(string value) 
            : base((byte)PropertyID.AuthenticationMethod, value)
        {
        }
    }
}
