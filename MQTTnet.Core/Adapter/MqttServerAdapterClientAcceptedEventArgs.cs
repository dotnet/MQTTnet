using System;

namespace MQTTnet.Core.Adapter
{
    public class MqttServerAdapterClientAcceptedEventArgs : EventArgs
    {
        public MqttServerAdapterClientAcceptedEventArgs(IMqttCommunicationAdapter client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IMqttCommunicationAdapter Client { get; }
    }
}
