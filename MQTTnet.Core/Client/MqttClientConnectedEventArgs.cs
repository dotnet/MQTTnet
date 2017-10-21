using System;

namespace MQTTnet.Core.Client
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(bool isSessionPresent)
        {
            IsSessionPresent = isSessionPresent;
        }

        public bool IsSessionPresent { get; }
    }
}
