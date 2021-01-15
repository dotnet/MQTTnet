using System;
using MQTTnet.Client.Connecting;

namespace MQTTnet.Client.Disconnecting
{
    public sealed class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception, MqttClientAuthenticateResult authenticateResult, MqttClientDisconnectReason reason)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
            AuthenticateResult = authenticateResult;
            Reason = reason;

            ReasonCode = reason;
        }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }

        /// <summary>
        /// Gets the authentication result.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientAuthenticateResult AuthenticateResult { get; }

        /// <summary>
        /// Gets or sets the reason.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientDisconnectReason Reason { get; set; }

        [Obsolete("Please use 'Reason' instead. This property will be removed in the future!")]
        public MqttClientDisconnectReason ReasonCode { get; set; }
    }
}
