using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttClientMessageQueueInterceptorDelegate : IMqttServerClientMessageQueueInterceptor
    {
        readonly Func<MqttClientMessageQueueInterceptorContext, Task> _callback;

        public MqttClientMessageQueueInterceptorDelegate(Action<MqttClientMessageQueueInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
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