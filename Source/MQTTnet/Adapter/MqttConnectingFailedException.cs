using System;
using MQTTnet.Client.Connecting;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter
{
    public sealed class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(string message, Exception innerException, MqttClientAuthenticateResult authenticateResult)
            : base(message, innerException)
        {
            Result = authenticateResult;
        }

        public MqttClientAuthenticateResult Result { get; }

        public MqttClientConnectResultCode ResultCode => Result?.ResultCode ?? MqttClientConnectResultCode.UnspecifiedError;
    }
}
