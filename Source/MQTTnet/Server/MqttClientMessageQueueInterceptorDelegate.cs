using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttClientMessageQueueInterceptorDelegate : IMqttServerClientMessageQueueInterceptor
    {
        private readonly Func<MqttClientMessageQueueInterceptorContext, Task> _callback;

        public MqttClientMessageQueueInterceptorDelegate(Action<MqttClientMessageQueueInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return Task.FromResult(0);
            };
        }

        public MqttClientMessageQueueInterceptorDelegate(Func<MqttClientMessageQueueInterceptorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task InterceptClientMessageQueueEnqueueAsync(MqttClientMessageQueueInterceptorContext context)
        {
            return _callback(context);
        }
    }
}