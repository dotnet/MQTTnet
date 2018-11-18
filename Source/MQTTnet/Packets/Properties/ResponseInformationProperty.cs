using MQTTnet.Packets.Properties.BaseTypes;

namespace MQTTnet.Packets.Properties
{
    public class ResponseInformationProperty : StringProperty
    {
        public ResponseInformationProperty(string value) 
            : base((byte)MqttMessagePropertyID.ResponseInformation, value)
        {
        }
    }
}
