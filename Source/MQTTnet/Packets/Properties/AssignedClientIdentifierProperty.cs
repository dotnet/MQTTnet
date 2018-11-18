using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class AssignedClientIdentifierProperty : StringProperty
    {
        public AssignedClientIdentifierProperty(string value) 
            : base((byte)MqttMessagePropertyID.AssignedClientIdentifer, value)
        {
        }
    }
}
