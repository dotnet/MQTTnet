using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerApplicationMessageInterceptorDelegate : IMqttServerApplicationMessageInterceptor
    {
        readonly Func<MqttApplicationMessageInterceptorContext, Task> _callback;

        public MqttServerApplicationMessageInterceptorDelegate(Action<MqttApplicationMessageInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
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
