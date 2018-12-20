using System;

namespace MQTTnet.Client.Connecting
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(MqttClientConnectResult connectResult)
        {
            ConnectResult = connectResult ?? throw new ArgumentNullException(nameof(connectResult));
        }

        public MqttClientConnectResult ConnectResult { get; }
    }
}
