using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Adapter
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
