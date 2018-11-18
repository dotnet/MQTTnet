using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ReasonStringProperty : StringProperty
    {
        public ReasonStringProperty(string value) 
            : base((byte)MqttMessagePropertyID.ReasonString, value)
        {
        }
    }
}
