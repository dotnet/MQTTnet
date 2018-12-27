using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerApplicationMessageInterceptorDelegate : IMqttServerApplicationMessageInterceptor
    {
        private readonly Func<MqttApplicationMessageInterceptorContext, Task> _callback;

        public MqttServerApplicationMessageInterceptorDelegate(Action<MqttApplicationMessageInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return Task.FromResult(0);
            };
        }

        public MqttServerApplicationMessageInterceptorDelegate(Func<MqttApplicationMessageInterceptorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            return _callback(context);
        }
    }
}
