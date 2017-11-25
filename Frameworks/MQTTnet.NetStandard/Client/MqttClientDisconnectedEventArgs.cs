using System;

namespace MQTTnet.Client
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected)
        {
            ClientWasConnected = clientWasConnected;
        }

        public bool ClientWasConnected { get; }
    }
}
