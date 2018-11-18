namespace MQTTnet.Packets.Properties
{
    public class RequestResponseInformationProperty : ByteProperty
    {
        public RequestResponseInformationProperty(byte value) 
            : base((byte)PropertyID.RequestResponseInformation, value)
        {
        }
    }
}
