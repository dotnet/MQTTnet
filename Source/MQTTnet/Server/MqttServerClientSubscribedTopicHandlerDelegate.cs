using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    [Obsolete("Use MqttServerClientSubscribedTopicHandlerDelegate instead. This will be removed in a future version.")]
    public sealed class MqttServerClientSubscribedHandlerDelegate : MqttServerClientSubscribedTopicHandlerDelegate
    {
        public MqttServerClientSubscribedHandlerDelegate(Action<MqttServerClientSubscribedTopicEventArgs> handler) : base(handler)
        {
        }

        public MqttServerClientSubscribedHandlerDelegate(Func<MqttServerClientSubscribedTopicEventArgs, Task> handler) : base(handler)
        {
        }
    }
    
    public class MqttServerClientSubscribedTopicHandlerDelegate : IMqttServerClientSubscribedTopicHandler
    {
        readonly Func<MqttServerClientSubscribedTopicEventArgs, Task> _handler;

        public MqttServerClientSubscribedTopicHandlerDelegate(Action<MqttServerClientSubscribedTopicEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerClientSubscribedTopicHandlerDelegate(Func<MqttServerClientSubscribedTopicEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
