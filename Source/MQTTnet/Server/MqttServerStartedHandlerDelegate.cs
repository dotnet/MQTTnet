using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerStartedHandlerDelegate : IMqttServerStartedHandler
    {
        private readonly Func<EventArgs, Task> _handler;

        public MqttServerStartedHandlerDelegate(Action<EventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
            };
        }

        public MqttServerStartedHandlerDelegate(Func<EventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleServerStartedAsync(EventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
