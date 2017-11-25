using System;

namespace MQTTnet.Server
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(ConnectedMqttClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        public ConnectedMqttClient Client { get; }
    }
}
