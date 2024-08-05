// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace MQTTnet.AspNetCore;

public sealed class SocketReceiver
{
    readonly SocketAwaitable _awaitable;
    readonly SocketAsyncEventArgs _eventArgs = new();
    readonly Socket _socket;

    public SocketReceiver(Socket socket, PipeScheduler scheduler)
    {
        _socket = socket;
        _awaitable = new SocketAwaitable(scheduler);
        _eventArgs.UserToken = _awaitable;
        _eventArgs.Completed += (_, e) => ((SocketAwaitable)e.UserToken).Complete(e.BytesTransferred, e.SocketError);
    }

    public SocketAwaitable ReceiveAsync(Memory<byte> buffer)
    {
        _eventArgs.SetBuffer(buffer);

        if (!_socket.ReceiveAsync(_eventArgs))
        {
            _awaitable.Complete(_eventArgs.BytesTransferred, _eventArgs.SocketError);
        }

        return _awaitable;
    }
}