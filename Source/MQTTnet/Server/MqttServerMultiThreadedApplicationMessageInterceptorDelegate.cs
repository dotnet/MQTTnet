using System;
using System.Threading.Tasks;
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

        public Func<MqttApplicationMessageInterceptorContext, Exception, Task> ExceptionHandler { get; set; }

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
                    var exceptionHandler = ExceptionHandler;
                    if (exceptionHandler != null)
                    {
                        await exceptionHandler.Invoke(context, exception).ConfigureAwait(false);
                    }
                }
            }).RunInBackground();

            return PlatformAbstractionLayer.CompletedTask;
        }
    }
}