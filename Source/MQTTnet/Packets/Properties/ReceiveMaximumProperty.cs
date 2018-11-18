using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ReceiveMaximumProperty : TwoByteIntegerProperty
    {
        public ReceiveMaximumProperty(ushort value) 
            : base((byte)MqttMessagePropertyID.ReceiveMaximum, value)
        {
        }
    }
}
