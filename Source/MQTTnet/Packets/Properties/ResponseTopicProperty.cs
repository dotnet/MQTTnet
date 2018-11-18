namespace MQTTnet.Packets.Properties
{
    public class ResponseTopicProperty : StringProperty
    {
        public ResponseTopicProperty(string value) 
            : base((byte)PropertyID.ResponseTopic, value)
        {
        }
    }
}
