using System;
using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public class MqttApplicationMessageReceivedHandlerDelegate : IMqttApplicationMessageReceivedHandler
    {
        private readonly Func<MqttApplicationMessageReceivedEventArgs, ValueTask> _handler;

        public MqttApplicationMessageReceivedHandlerDelegate(Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return new ValueTask();
            };
        }

        public MqttApplicationMessageReceivedHandlerDelegate(Func<MqttApplicationMessageReceivedEventArgs, ValueTask> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        
        public ValueTask HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs context)
        {
            return _handler(context);
        }
    }
}
