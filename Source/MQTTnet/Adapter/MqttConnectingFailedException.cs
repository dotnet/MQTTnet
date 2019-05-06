using MQTTnet.Client.Connecting;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter
{
    public class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(MqttClientConnectResultCode resultCode)
            : base($"Connecting with MQTT server failed ({resultCode.ToString()}).")
        {
            ResultCode = resultCode;
        }

        public MqttClientConnectResultCode ResultCode { get; }
    }
}
