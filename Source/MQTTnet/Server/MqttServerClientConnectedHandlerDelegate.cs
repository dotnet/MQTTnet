using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerClientConnectedHandlerDelegate : IMqttServerClientConnectedHandler
    {
        readonly Func<MqttServerClientConnectedEventArgs, Task> _handler;

        public MqttServerClientConnectedHandlerDelegate(Action<MqttServerClientConnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerClientConnectedHandlerDelegate(Func<MqttServerClientConnectedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
