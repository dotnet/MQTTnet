using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerUnsubscriptionInterceptorDelegate : IMqttServerUnsubscriptionInterceptor
    {
        readonly Func<MqttUnsubscriptionInterceptorContext, Task> _callback;

        public MqttServerUnsubscriptionInterceptorDelegate(Action<MqttUnsubscriptionInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerUnsubscriptionInterceptorDelegate(Func<MqttUnsubscriptionInterceptorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task InterceptUnsubscriptionAsync(MqttUnsubscriptionInterceptorContext context)
        {
            return _callback(context);
        }
    }
}
