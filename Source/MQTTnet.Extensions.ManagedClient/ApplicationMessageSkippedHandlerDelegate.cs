// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ApplicationMessageSkippedHandlerDelegate : IApplicationMessageSkippedHandler
    {
        private readonly Func<ApplicationMessageSkippedEventArgs, Task> _handler;

        public ApplicationMessageSkippedHandlerDelegate(Action<ApplicationMessageSkippedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
            };
        }

        public ApplicationMessageSkippedHandlerDelegate(Func<ApplicationMessageSkippedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleApplicationMessageSkippedAsync(ApplicationMessageSkippedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
