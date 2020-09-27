using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerClientDisconnectedHandlerDelegate : IMqttServerClientDisconnectedHandler
    {
        private readonly Func<MqttServerClientDisconnectedEventArgs, Task> _handler;

        public MqttServerClientDisconnectedHandlerDelegate(Action<MqttServerClientDisconnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
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
