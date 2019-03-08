using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public class MqttApplicationMessageReceivedHandlerDelegate : IMqttApplicationMessageReceivedHandler
    {
        private readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handler;

        public MqttApplicationMessageReceivedHandlerDelegate(Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
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
