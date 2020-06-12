using System;
using MQTTnet.Client.Connecting;

namespace MQTTnet.Client.Disconnecting
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception, MqttClientAuthenticateResult authenticateResult, MqttClientDisconnectReason reasonCode)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
            AuthenticateResult = authenticateResult;
            ReasonCode = reasonCode;
        }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }

        public MqttClientAuthenticateResult AuthenticateResult { get; }

        public MqttClientDisconnectReason ReasonCode { get; set; } = MqttClientDisconnectReason.NormalDisconnection;
    }
}
