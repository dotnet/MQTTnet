using System;

namespace MQTTnet.Core.Adapter
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public MqttClientDisconnectedEventArgs(string identifier, IMqttCommunicationAdapter clientAdapter)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            ClientAdapter = clientAdapter ?? throw new ArgumentNullException(nameof(clientAdapter));
        }

        public string Identifier { get; }

        public IMqttCommunicationAdapter ClientAdapter { get; }
    }
}
