// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ConnectingFailedHandlerDelegate : IConnectingFailedHandler
    {
        private readonly Func<ManagedProcessFailedEventArgs, Task> _handler;

        public ConnectingFailedHandlerDelegate(Action<ManagedProcessFailedEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _handler = eventArgs =>
            {
                handler(eventArgs);
                return Task.FromResult(0);
            };
        }

        public ConnectingFailedHandlerDelegate(Func<ManagedProcessFailedEventArgs, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public Task HandleConnectingFailedAsync(ManagedProcessFailedEventArgs eventArgs)
        {
            return _handler(eventArgs);
        }
    }
}
