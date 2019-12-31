using MQTTnet.Client.Connecting;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter
{
    public class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(MqttClientAuthenticateResult resultCode)
            : base($"Connecting with MQTT server failed ({resultCode.ToString()}).")
        {
            Result = resultCode;
        }

        public MqttClientAuthenticateResult Result { get; }
        public MqttClientConnectResultCode ResultCode => Result.ResultCode;
    }
}
