// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MQTTnet.AspNetCore.Client.Tcp;

public sealed class SocketSender
{
    readonly SocketAwaitable _awaitable;
    readonly SocketAsyncEventArgs _eventArgs = new();
    readonly Socket _socket;

    List<ArraySegment<byte>> _bufferList;

    public SocketSender(Socket socket, PipeScheduler scheduler)
    {
        _socket = socket;
        _awaitable = new SocketAwaitable(scheduler);
        _eventArgs.UserToken = _awaitable;
        _eventArgs.Completed += (_, e) => ((SocketAwaitable)e.UserToken).Complete(e.BytesTransferred, e.SocketError);
    }

    public SocketAwaitable SendAsync(in ReadOnlySequence<byte> buffers)
    {
        if (buffers.IsSingleSegment)
        {
            return SendAsync(buffers.First);
        }

        if (!_eventArgs.MemoryBuffer.Equals(Memory<byte>.Empty))
        {
            _eventArgs.SetBuffer(null, 0, 0);
        }

        _eventArgs.BufferList = GetBufferList(buffers);

        if (!_socket.SendAsync(_eventArgs))
        {
            _awaitable.Complete(_eventArgs.BytesTransferred, _eventArgs.SocketError);
        }

        return _awaitable;
    }

    List<ArraySegment<byte>> GetBufferList(in ReadOnlySequence<byte> buffer)
    {
        Debug.Assert(!buffer.IsEmpty);
        Debug.Assert(!buffer.IsSingleSegment);

        if (_bufferList == null)
        {
            _bufferList = new List<ArraySegment<byte>>();
        }
        else
        {
            // Buffers are pooled, so it's OK to root them until the next multi-buffer write.
            _bufferList.Clear();
        }

        foreach (var b in buffer)
        {
            _bufferList.Add(b.GetArray());
        }

        return _bufferList;
    }

    SocketAwaitable SendAsync(ReadOnlyMemory<byte> memory)
    {
        // The BufferList getter is much less expensive then the setter.
        if (_eventArgs.BufferList != null)
        {
            _eventArgs.BufferList = null;
        }

        _eventArgs.SetBuffer(MemoryMarshal.AsMemory(memory));

        if (!_socket.SendAsync(_eventArgs))
        {
            _awaitable.Complete(_eventArgs.BytesTransferred, _eventArgs.SocketError);
        }

        return _awaitable;
    }
}