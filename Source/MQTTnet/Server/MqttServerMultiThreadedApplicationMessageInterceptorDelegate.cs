using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttServerMultiThreadedApplicationMessageInterceptorDelegate : IMqttServerApplicationMessageInterceptor
    {
        readonly Func<MqttApplicationMessageInterceptorContext, Task> _callback;

        public MqttServerMultiThreadedApplicationMessageInterceptorDelegate(Action<MqttApplicationMessageInterceptorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerMultiThreadedApplicationMessageInterceptorDelegate(Func<MqttApplicationMessageInterceptorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _callback.Invoke(context).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    context.Logger.Error(exception, "Error while intercepting application message.");
                }
            }).RunInBackground();

            return PlatformAbstractionLayer.CompletedTask;
        }
    }
}