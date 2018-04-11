using System;

namespace MQTTnet.Server
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(ConnectedMqttClient client, bool wasCleanDisconnect)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            WasCleanDisconnect = wasCleanDisconnect;
        }
        
        public ConnectedMqttClient Client { get; }

        public bool WasCleanDisconnect { get; }
    }
}
