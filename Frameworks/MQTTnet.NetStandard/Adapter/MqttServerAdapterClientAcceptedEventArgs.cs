using System;
using System.Threading.Tasks;

namespace MQTTnet.Adapter
{
    public class MqttServerAdapterClientAcceptedEventArgs : EventArgs
    {
        public MqttServerAdapterClientAcceptedEventArgs(IMqttChannelAdapter client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IMqttChannelAdapter Client { get; }

        public Task SessionTask { get; set; }
    }
}
