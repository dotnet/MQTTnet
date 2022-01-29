using System;

namespace MQTTnet.Client
{
    public sealed class MqttClientConnectingEventArgs : EventArgs
    {
        public MqttClientConnectingEventArgs(IMqttClientOptions clientOptions)
        {
            ClientOptions = clientOptions;
        }

        public IMqttClientOptions ClientOptions { get; }
    }
}