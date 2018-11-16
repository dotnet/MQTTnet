namespace MQTTnet.Packets.Properties
{
    public class Property
    {
        public PropertyType Type { get; set; }

        public IPropertyValue Value { get; set; }
    }
}
