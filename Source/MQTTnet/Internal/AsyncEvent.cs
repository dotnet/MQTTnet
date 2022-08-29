// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.Internal
{
    public sealed class AsyncEvent<TEventArgs> where TEventArgs : EventArgs
    {
        readonly List<AsyncEventInvocator<TEventArgs>> _handlers = new List<AsyncEventInvocator<TEventArgs>>();

        public bool HasHandlers => _handlers.Count > 0;

        public void AddHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(new AsyncEventInvocator<TEventArgs>(null, handler));
        }

        public void AddHandler(Action<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(new AsyncEventInvocator<TEventArgs>(handler, null));
        }

        public async Task InvokeAsync(TEventArgs eventArgs)
        {
            foreach (var handler in _handlers.ToArray())
            {
                await handler.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
        }
        
        public void RemoveHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.RemoveAll(h => h.WrapsHandler(handler));
        }

        public void RemoveHandler(Action<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.RemoveAll(h => h.WrapsHandler(handler));
        }

        public async Task TryInvokeAsync(TEventArgs eventArgs, MqttNetSourceLogger logger)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                await InvokeAsync(eventArgs).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.Warning(exception, $"Error while invoking event ({typeof(TEventArgs)}).");
            }
        }
    }
}
