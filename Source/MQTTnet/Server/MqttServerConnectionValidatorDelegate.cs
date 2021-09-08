using System;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Server
{
    public sealed class MqttServerConnectionValidatorDelegate : IMqttServerConnectionValidator
    {
        readonly Func<MqttConnectionValidatorContext, Task> _callback;

        public MqttServerConnectionValidatorDelegate(Action<MqttConnectionValidatorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return PlatformAbstractionLayer.CompletedTask;
            };
        }

        public MqttServerConnectionValidatorDelegate(Func<MqttConnectionValidatorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task ValidateConnectionAsync(MqttConnectionValidatorContext context)
        {
            return _callback(context);
        }
    }
}
