using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ServerReferenceProperty : StringProperty
    {
        public ServerReferenceProperty(string value) 
            : base((byte)MqttMessagePropertyID.ServerReference, value)
        {
        }
    }
}
