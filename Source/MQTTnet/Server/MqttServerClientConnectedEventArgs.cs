using System;

namespace MQTTnet.Server
{
    public class MqttServerClientConnectedEventArgs : EventArgs
    {
        public MqttServerClientConnectedEventArgs(string clientId)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public string ClientId { get; }
    }
}
