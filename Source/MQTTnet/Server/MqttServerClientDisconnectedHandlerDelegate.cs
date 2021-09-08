using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerClientDisconnectedHandlerDelegate : IMqttServerClientDisconnectedHandler
    {
        readonly Func<MqttServerClientDisconnectedEventArgs, Task> _handler;

        public MqttServerClientDisconnectedHandlerDelegate(Action<MqttServerClientDisconnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerClientDisconnectedHandlerDelegate(Func<MqttServerClientDisconnectedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
