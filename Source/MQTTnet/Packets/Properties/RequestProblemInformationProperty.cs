namespace MQTTnet.Packets.Properties
{
    public class RequestProblemInformationProperty : ByteProperty
    {
        public RequestProblemInformationProperty(byte value) 
            : base((byte)PropertyID.RequestProblemInformation, value)
        {
        }
    }
}
