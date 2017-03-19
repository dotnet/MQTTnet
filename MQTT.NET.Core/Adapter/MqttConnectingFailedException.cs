using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Adapter
{
    public class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(MqttConnectReturnCode returnCode) 
            : base($"Connecting with MQTT server failed ({returnCode}).")
        {
            ReturnCode = returnCode;
        }

        public MqttConnectReturnCode ReturnCode { get; }
    }
}
