using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerClientUnsubscribedTopicHandlerDelegate : IMqttServerClientUnsubscribedTopicHandler
    {
        private readonly Func<MqttServerClientUnsubscribedTopicEventArgs, Task> _handler;

        public MqttServerClientUnsubscribedTopicHandlerDelegate(Action<MqttServerClientUnsubscribedTopicEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
            };
        }

        public MqttServerClientUnsubscribedTopicHandlerDelegate(Func<MqttServerClientUnsubscribedTopicEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleClientUnsubscribedTopicAsync(MqttServerClientUnsubscribedTopicEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
