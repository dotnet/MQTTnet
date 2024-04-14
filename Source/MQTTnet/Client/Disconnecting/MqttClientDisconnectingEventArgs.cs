using System;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttClientDisconnectingEventArgs : EventArgs
    {
        public MqttClientDisconnectingEventArgs(
            MqttDisconnectReasonCode reason,
            bool clientWasConnected,
            Exception exception,
            MqttClientConnectResult connectResult)
        {
            Reason = reason;
            ClientWasConnected = clientWasConnected;
            Exception = exception;
            ConnectResult = connectResult;
        }

        /// <summary>
        ///     Gets or sets the reason.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttDisconnectReasonCode Reason { get; }

        public bool ClientWasConnected { get; }

        public Exception Exception { get; }

        /// <summary>
        ///     Gets the authentication result.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; }
    }
}
