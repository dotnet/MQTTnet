using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ContentTypeProperty : StringProperty
    {
        public ContentTypeProperty(string value) 
            : base((byte)MqttMessagePropertyID.ContentType, value)
        {
        }
    }
}
