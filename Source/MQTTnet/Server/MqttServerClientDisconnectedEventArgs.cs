using System;

namespace MQTTnet.Server
{
    public class MqttServerClientDisconnectedEventArgs : EventArgs
    {
        public MqttServerClientDisconnectedEventArgs(string clientId, MqttClientDisconnectType disconnectType)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            DisconnectType = disconnectType;
        }
        
        public string ClientId { get; }

        public MqttClientDisconnectType DisconnectType { get; }
    }
}
