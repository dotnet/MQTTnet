using MQTTnet.Core.Server;
using System;

namespace MQTTnet.Core.Adapter
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
