using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public class MqttApplicationMessageHandlerDelegate : IMqttApplicationMessageHandler
    {
        private readonly Func<MqttApplicationMessageHandlerContext, Task> _handler;

        public MqttApplicationMessageHandlerDelegate(Action<MqttApplicationMessageHandlerContext> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public MqttApplicationMessageHandlerDelegate(Func<MqttApplicationMessageHandlerContext, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleApplicationMessageAsync(MqttApplicationMessageHandlerContext context)
        {
            return _handler(context);
        }
    }
}
