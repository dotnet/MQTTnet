using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public class MqttServerClientDisconnectedHandlerDelegate : IMqttServerClientDisconnectedHandler
    {
        private readonly Func<MqttServerClientDisconnectedEventArgs, Task> _callback;

        public MqttServerClientDisconnectedHandlerDelegate(Action<MqttServerClientDisconnectedEventArgs> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            _callback = eventArgs =>
            {
                callback(eventArgs);
                return Task.FromResult(0);
            };
        }

        public MqttServerClientDisconnectedHandlerDelegate(Func<MqttServerClientDisconnectedEventArgs, Task> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            return _callback(eventArgs);
        }
    }
}
