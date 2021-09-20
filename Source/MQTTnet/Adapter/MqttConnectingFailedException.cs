using System;
using MQTTnet.Client.Connecting;
using MQTTnet.Exceptions;

namespace MQTTnet.Adapter
{
    public sealed class MqttConnectingFailedException : MqttCommunicationException
    {
        public MqttConnectingFailedException(string message, Exception innerException, MqttClientConnectResult connectResult)
            : base(message, innerException)
        {
            Result = connectResult;
        }

        public MqttClientConnectResult Result { get; }

        public MqttClientConnectResultCode ResultCode => Result?.ResultCode ?? MqttClientConnectResultCode.UnspecifiedError;
    }
}
