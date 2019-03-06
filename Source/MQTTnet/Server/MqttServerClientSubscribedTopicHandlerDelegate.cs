using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerClientSubscribedHandlerDelegate : IMqttServerClientSubscribedTopicHandler
    {
        private readonly Func<MqttServerClientSubscribedTopicEventArgs, Task> _handler;

        public MqttServerClientSubscribedHandlerDelegate(Action<MqttServerClientSubscribedTopicEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
            };
        }

        public MqttServerClientSubscribedHandlerDelegate(Func<MqttServerClientSubscribedTopicEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
