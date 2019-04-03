using System;
using System.Threading.Tasks;

namespace MQTTnet.Adapter
{
    public class MqttServerAdapterClientAcceptedEventArgs : EventArgs
    {
        public MqttServerAdapterClientAcceptedEventArgs(IMqttChannelAdapter channelAdapter)
        {
            ChannelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
        }

        public IMqttChannelAdapter ChannelAdapter { get; }

        public Task SessionTask { get; set; }
    }
}
