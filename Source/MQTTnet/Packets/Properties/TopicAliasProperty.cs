using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class TopicAliasProperty : TwoByteIntegerProperty
    {
        public TopicAliasProperty(ushort value) 
            : base((byte)MqttMessagePropertyID.TopicAlias, value)
        {
        }
    }
}
