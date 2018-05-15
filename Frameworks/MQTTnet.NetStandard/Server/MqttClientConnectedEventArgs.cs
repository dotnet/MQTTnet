using System;

namespace MQTTnet.Server
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(string clientId)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public string ClientId { get; }
    }
}
