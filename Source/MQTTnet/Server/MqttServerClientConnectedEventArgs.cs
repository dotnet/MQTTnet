using System;

namespace MQTTnet.Server
{
    public class MqttServerClientConnectedEventArgs : EventArgs
    {
        public MqttServerClientConnectedEventArgs(string clientId)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }
    }
}
