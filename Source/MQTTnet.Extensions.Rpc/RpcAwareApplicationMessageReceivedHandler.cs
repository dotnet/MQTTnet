using System;
using System.Threading.Tasks;
using MQTTnet.Client.Receiving;

namespace MQTTnet.Extensions.Rpc
{
    public class RpcAwareApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _handleReceivedApplicationMessageAsync;

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
            if (OriginalHandler != null)
            {
                await OriginalHandler.HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
            }

            await _handleReceivedApplicationMessageAsync(eventArgs).ConfigureAwait(false);
        }
    }
}
