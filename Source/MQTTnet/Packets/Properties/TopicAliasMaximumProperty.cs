using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class TopicAliasMaximumProperty : TwoByteIntegerProperty
    {
        public TopicAliasMaximumProperty(ushort value)
            : base((byte)MqttMessagePropertyID.TopicAliasMaximum, value)
        {
        }
    }
}
