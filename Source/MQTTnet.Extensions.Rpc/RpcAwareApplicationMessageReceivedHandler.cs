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

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // First try to check if there is a pending RPC call! 
            await _handleReceivedApplicationMessageAsync(eventArgs).ConfigureAwait(false);
            
            if (OriginalHandler != null)
            {
                await OriginalHandler.HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
            }
        }
    }
}
