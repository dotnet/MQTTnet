using MQTTnet.Internal;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerConnectionValidatorDelegate : IMqttServerConnectionValidator
    {
        private readonly Func<MqttConnectionValidatorContext, Task> _callback;

        public MqttServerConnectionValidatorDelegate(Action<MqttConnectionValidatorContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = context =>
            {
                callback(context);
                return MqttTask.Completed;
            };
        }

        public MqttServerConnectionValidatorDelegate(Func<MqttConnectionValidatorContext, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task ValidateConnection(MqttConnectionValidatorContext context)
        {
            return _callback(context);
        }
    }
}
