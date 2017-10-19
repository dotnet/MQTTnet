using System;

namespace MQTTnet.Core.Client
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
