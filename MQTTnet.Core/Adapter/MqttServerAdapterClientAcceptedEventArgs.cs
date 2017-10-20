using System;
using System.Threading.Tasks;

namespace MQTTnet.Core.Adapter
{
    public class MqttServerAdapterClientAcceptedEventArgs : EventArgs
    {
        public MqttServerAdapterClientAcceptedEventArgs(IMqttCommunicationAdapter client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IMqttCommunicationAdapter Client { get; }

        public Task SessionTask { get; set; }
    }
}
