using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerStoppedHandlerDelegate : IMqttServerStoppedHandler
    {
        readonly Func<EventArgs, Task> _handler;

        public MqttServerStoppedHandlerDelegate(Action<EventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerStoppedHandlerDelegate(Func<EventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleServerStoppedAsync(EventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
