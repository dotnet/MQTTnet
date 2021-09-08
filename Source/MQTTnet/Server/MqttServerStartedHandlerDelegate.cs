using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerStartedHandlerDelegate : IMqttServerStartedHandler
    {
        readonly Func<EventArgs, Task> _handler;

        public MqttServerStartedHandlerDelegate(Action<EventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
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
