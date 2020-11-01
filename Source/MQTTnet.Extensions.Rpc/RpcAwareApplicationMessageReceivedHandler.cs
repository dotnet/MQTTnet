using System;
using System.Threading.Tasks;
using MQTTnet.Client.Receiving;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class RpcAwareApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handleReceivedApplicationMessageAsync;

        public RpcAwareApplicationMessageReceivedHandler(
            IMqttApplicationMessageReceivedHandler originalHandler,
            Func<MqttApplicationMessageReceivedEventArgs, Task> handleReceivedApplicationMessageAsync)
        {
            OriginalHandler = originalHandler;
            _handleReceivedApplicationMessageAsync = handleReceivedApplicationMessageAsync ?? throw new ArgumentNullException(nameof(handleReceivedApplicationMessageAsync));
        }

        public IMqttApplicationMessageReceivedHandler OriginalHandler { get; }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (OriginalHandler != null)
            {
                return OriginalHandler.HandleApplicationMessageReceivedAsync(eventArgs);
            }

            return _handleReceivedApplicationMessageAsync(eventArgs);
        }
    }
}
