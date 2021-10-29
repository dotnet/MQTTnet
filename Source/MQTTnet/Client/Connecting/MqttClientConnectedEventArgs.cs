using System;

namespace MQTTnet.Client
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(MqttClientConnectResult connectResult)
        {
            ConnectResult = connectResult ?? throw new ArgumentNullException(nameof(connectResult));
        }

        /// <summary>
        /// Gets the authentication result.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; }
    }
}
