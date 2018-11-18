namespace MQTTnet.Packets.Properties
{
    public class ContentTypeProperty : StringProperty
    {
        public ContentTypeProperty(string value) 
            : base((byte)PropertyID.ContentType, value)
        {
        }
    }
}
