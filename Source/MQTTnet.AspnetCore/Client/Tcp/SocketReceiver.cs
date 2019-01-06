// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MQTTnet.AspNetCore.Client.Tcp;
using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public sealed class SocketReceiver : SocketSenderReceiverBase
    {
        public SocketReceiver(Socket socket, PipeScheduler scheduler) : base(socket, scheduler)
        {
        }

        public SocketAwaitableEventArgs WaitForDataAsync()
        {

#if NETCOREAPP2_2
            _awaitableEventArgs.SetBuffer(Memory<byte>.Empty);
#else
            _awaitableEventArgs.SetBuffer(new byte[0], 0, 0);
#endif

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
#if NETCOREAPP2_2
            _awaitableEventArgs.SetBuffer(buffer);
#else
            var segment = buffer.GetArray();
            _awaitableEventArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
#endif

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }
    }
}