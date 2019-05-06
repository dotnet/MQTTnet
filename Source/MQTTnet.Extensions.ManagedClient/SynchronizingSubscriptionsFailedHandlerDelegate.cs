using System;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public class SynchronizingSubscriptionsFailedHandlerDelegate : ISynchronizingSubscriptionsFailedHandler
    {
        private readonly Func<ManagedProcessFailedEventArgs, Task> _handler;

        public SynchronizingSubscriptionsFailedHandlerDelegate(Action<ManagedProcessFailedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = context =>
            {
                handler(context);
                return Task.FromResult(0);
            };
        }

        public SynchronizingSubscriptionsFailedHandlerDelegate(Func<ManagedProcessFailedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleSynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
