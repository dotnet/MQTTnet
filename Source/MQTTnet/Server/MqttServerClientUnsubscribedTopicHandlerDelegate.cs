using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerClientUnsubscribedTopicHandlerDelegate : IMqttServerClientUnsubscribedTopicHandler
    {
        readonly Func<MqttServerClientUnsubscribedTopicEventArgs, Task> _handler;

        public MqttServerClientUnsubscribedTopicHandlerDelegate(Action<MqttServerClientUnsubscribedTopicEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
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
