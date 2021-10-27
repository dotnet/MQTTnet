using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncEvent<TEventArgs> where TEventArgs : EventArgs
    {
        readonly List<Func<TEventArgs, Task>> _handlers = new List<Func<TEventArgs, Task>>();

        public void AddHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            _handlers.Add(handler);
        }

        public void RemoveHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            _handlers.Remove(handler);
        }

        public async Task InvokeAsync(TEventArgs eventArgs)
        {
            foreach (var handler in _handlers)
            {
                await handler.Invoke(eventArgs);
            }
        }

        public async Task<TEventArgs> InvokeAsync(Func<TEventArgs> eventArgsProvider)
        {
            if (eventArgsProvider == null) throw new ArgumentNullException(nameof(eventArgsProvider));
            
            if (!_handlers.Any())
            {
                return default;
            }

            var eventArgs = eventArgsProvider.Invoke();
            await InvokeAsync(eventArgs).ConfigureAwait(false);
            return eventArgs;
        }
    }
}