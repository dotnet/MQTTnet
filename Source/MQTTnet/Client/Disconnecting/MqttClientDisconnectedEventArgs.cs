using System;

namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception, MqttClientConnectResult connectResult, MqttClientDisconnectReason reason)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
            ConnectResult = connectResult;
            Reason = reason;

#pragma warning disable 618
            ReasonCode = reason;
#pragma warning restore 618
        }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }

        /// <summary>
        /// Gets the authentication result.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; }

        /// <summary>
        /// Gets or sets the reason.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientDisconnectReason Reason { get; set; }

        [Obsolete("Please use 'Reason' instead. This property will be removed in the future!")]
        public MqttClientDisconnectReason ReasonCode { get; set; }
    }
}
