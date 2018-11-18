using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class RequestProblemInformationProperty : ByteProperty
    {
        public RequestProblemInformationProperty(byte value) 
            : base((byte)MqttMessagePropertyID.RequestProblemInformation, value)
        {
        }
    }
}
