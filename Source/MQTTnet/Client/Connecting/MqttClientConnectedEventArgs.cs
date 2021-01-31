using System;

namespace MQTTnet.Client.Connecting
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(MqttClientAuthenticateResult authenticateResult)
        {
            AuthenticateResult = authenticateResult ?? throw new ArgumentNullException(nameof(authenticateResult));
        }

        /// <summary>
        /// Gets the authentication result.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientAuthenticateResult AuthenticateResult { get; }
    }
}
