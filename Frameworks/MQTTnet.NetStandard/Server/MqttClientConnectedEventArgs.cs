using System;

namespace MQTTnet.Server
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(ConnectedMqttClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public ConnectedMqttClient Client { get; }
    }
}
