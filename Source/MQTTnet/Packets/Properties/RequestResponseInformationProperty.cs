using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class RequestResponseInformationProperty : ByteProperty
    {
        public RequestResponseInformationProperty(byte value) 
            : base((byte)MqttMessagePropertyID.RequestResponseInformation, value)
        {
        }
    }
}
