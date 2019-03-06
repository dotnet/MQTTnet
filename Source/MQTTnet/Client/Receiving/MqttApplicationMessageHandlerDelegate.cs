using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public class MqttApplicationMessageHandlerDelegate : IMqttApplicationMessageHandler
    {
        private readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handler;

        public MqttApplicationMessageHandlerDelegate(Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public MqttApplicationMessageHandlerDelegate(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleApplicationMessageAsync(MqttApplicationMessageReceivedEventArgs context)
        {
            return _handler(context);
        }
    }
}
