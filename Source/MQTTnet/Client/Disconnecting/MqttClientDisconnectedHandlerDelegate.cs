using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Disconnecting
{
    public class MqttClientDisconnectedHandlerDelegate : IMqttClientDisconnectedHandler
    {
        private readonly Func<MqttClientDisconnectedEventArgs, Task> _handler;

        public MqttClientDisconnectedHandlerDelegate(Action<MqttClientDisconnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public MqttClientDisconnectedHandlerDelegate(Func<MqttClientDisconnectedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
