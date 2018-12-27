using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerSubscriptionInterceptorDelegate : IMqttServerSubscriptionInterceptor
    {
        private readonly Func<MqttSubscriptionInterceptorContext, Task> _callback;

        public MqttServerSubscriptionInterceptorDelegate(Action<MqttSubscriptionInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return Task.FromResult(0);
            };
        }

        public MqttServerSubscriptionInterceptorDelegate(Func<MqttSubscriptionInterceptorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context)
        {
            return _callback(context);
        }
    }
}
