using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class PayloadFormatIndicatorProperty : ByteProperty
    {
        public PayloadFormatIndicatorProperty(byte value) 
            : base((byte)MqttMessagePropertyID.PayloadFormatIndicator, value)
        {
        }
    }
}
