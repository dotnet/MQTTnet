using MQTTnet.Client.Connecting;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter
{
    public class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(MqttClientAuthenticateResult result)
            : base($"Connecting with MQTT server failed ({result.ResultCode.ToString()}).")
        {
            Result = result;
        }

        public MqttClientAuthenticateResult Result { get; }
        public MqttClientConnectResultCode ResultCode => Result.ResultCode;
    }
}
