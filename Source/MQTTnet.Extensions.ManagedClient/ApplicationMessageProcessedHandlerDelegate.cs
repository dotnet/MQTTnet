using System;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ApplicationMessageProcessedHandlerDelegate : IApplicationMessageProcessedHandler
    {
        private readonly Func<ApplicationMessageProcessedEventArgs, Task> _handler;

        public ApplicationMessageProcessedHandlerDelegate(Action<ApplicationMessageProcessedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public ApplicationMessageProcessedHandlerDelegate(Func<ApplicationMessageProcessedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
