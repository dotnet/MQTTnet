using System;

namespace MQTTnet.Server
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(string clientId, bool wasCleanDisconnect)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            WasCleanDisconnect = wasCleanDisconnect;
        }
        
        public string ClientId { get; }

        public bool WasCleanDisconnect { get; }
    }
}
