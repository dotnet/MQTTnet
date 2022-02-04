using System;

namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectedEventArgs : EventArgs
    {
        public bool ClientWasConnected { get; internal set; }

        public Exception Exception { get; internal set; }

        /// <summary>
        /// Gets the authentication result.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; internal set; }

        /// <summary>
        /// Gets or sets the reason.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientDisconnectReason Reason { get; internal set; }
        
        public string ReasonString { get; internal set; }
    }
}
