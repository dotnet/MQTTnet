using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ConnectingFailedEventArgs : EventArgs
    {
        public ConnectingFailedEventArgs(MqttClientConnectResult connectResult, Exception exception)
        {
            ConnectResult = connectResult;
            Exception = exception;
        }

        /// <summary>
        /// This is null when the connection was failing and the server was not reachable.
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; }

        public Exception Exception { get; }
    }
}