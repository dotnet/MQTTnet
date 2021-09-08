using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Client.Receiving
{
    public sealed class MqttApplicationMessageReceivedHandlerDelegate : IMqttApplicationMessageReceivedHandler
    {
        readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handler;

        public MqttApplicationMessageReceivedHandlerDelegate(Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttApplicationMessageReceivedHandlerDelegate(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs context)
        {
            return _handler(context);
        }
    }
}
