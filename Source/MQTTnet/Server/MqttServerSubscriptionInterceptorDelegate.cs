using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerSubscriptionInterceptorDelegate : IMqttServerSubscriptionInterceptor
    {
        readonly Func<MqttSubscriptionInterceptorContext, Task> _callback;

        public MqttServerSubscriptionInterceptorDelegate(Action<MqttSubscriptionInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
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
