using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ResponseTopicProperty : StringProperty
    {
        public ResponseTopicProperty(string value) 
            : base((byte)MqttMessagePropertyID.ResponseTopic, value)
        {
        }
    }
}
