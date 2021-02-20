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

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        public MqttClientDisconnectType DisconnectType { get; }
    }
}
