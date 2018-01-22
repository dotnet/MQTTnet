using System;

namespace MQTTnet.Client
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
        }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }
    }
}
