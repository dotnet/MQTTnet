using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerClientConnectedHandlerDelegate : IMqttServerClientConnectedHandler
    {
        private readonly Func<MqttServerClientConnectedEventArgs, Task> _handler;

        public MqttServerClientConnectedHandlerDelegate(Action<MqttServerClientConnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
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
