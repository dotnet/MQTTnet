using System;

namespace MQTTnet.Server
{
    public sealed class MqttServerClientDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        public MqttClientDisconnectType DisconnectType { get; internal set; }

        public string Endpoint { get; internal set; }
    }
}
