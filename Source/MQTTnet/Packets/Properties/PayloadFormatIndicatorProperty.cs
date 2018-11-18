namespace MQTTnet.Packets.Properties
{
    public class PayloadFormatIndicatorProperty : ByteProperty
    {
        public PayloadFormatIndicatorProperty(byte value) 
            : base((byte)PropertyID.PayloadFormatIndicator, value)
        {
        }
    }
}
