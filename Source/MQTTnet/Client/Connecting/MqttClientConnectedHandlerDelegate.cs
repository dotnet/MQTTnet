using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Connecting
{
    public class MqttClientConnectedHandlerDelegate : IMqttClientConnectedHandler
    {
        private readonly Func<MqttClientConnectedEventArgs, Task> _handler;

        public MqttClientConnectedHandlerDelegate(Action<MqttClientConnectedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public MqttClientConnectedHandlerDelegate(Func<MqttClientConnectedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
