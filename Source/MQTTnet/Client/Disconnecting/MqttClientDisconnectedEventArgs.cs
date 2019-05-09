using System;
using MQTTnet.Client.Connecting;

namespace MQTTnet.Client.Disconnecting
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception, MqttClientAuthenticateResult authenticateResult)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
            AuthenticateResult = authenticateResult;
        }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }

        public MqttClientAuthenticateResult AuthenticateResult { get; }
    }
}
