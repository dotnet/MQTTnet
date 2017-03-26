using System;

namespace MQTTnet.Core.Adapter
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public MqttClientConnectedEventArgs(string identifier, IMqttCommunicationAdapter clientAdapter)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            ClientAdapter = clientAdapter ?? throw new ArgumentNullException(nameof(clientAdapter));
        }

        public string Identifier { get; }

        public IMqttCommunicationAdapter ClientAdapter { get; }
    }
}
