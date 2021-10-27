using System;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public sealed class MqttServerClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client identifier of the connected client.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the user name of the connected client.
        /// </summary>
        public string UserName { get; internal set; }

        /// <summary>
        /// Gets the protocol version which is used by the connected client.
        /// </summary>
        public MqttProtocolVersion ProtocolVersion { get; internal set; }

        /// <summary>
        /// Gets the endpoint of the connected client.
        /// </summary>
        public string Endpoint { get; internal set; }
    }
}
