// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace MQTTnet.AspNetCore
{
    [DebuggerDisplay("Count = {Count}")]
    sealed class MqttBufferWriterPool : IReadOnlyCollection<MqttBufferWriterPool.ChannelMqttBufferWriter>
    {
        private readonly MqttServerOptions _serverOptions;
        private readonly IOptionsMonitor<MqttBufferWriterPoolOptions> _poolOptions;
        private readonly ConcurrentQueue<ChannelMqttBufferWriter> _bufferWriterQueue = new();

        public int Count => _bufferWriterQueue.Count;

        public MqttBufferWriterPool(
            MqttServerOptions serverOptions,
            IOptionsMonitor<MqttBufferWriterPoolOptions> poolOptions)
        {
            _serverOptions = serverOptions;
            _poolOptions = poolOptions;
        }

        public ChannelMqttBufferWriter Rent()
        {
            if (_bufferWriterQueue.TryDequeue(out var bufferWriter))
            {
                bufferWriter.Reset();
            }
            else
            {
                var writer = new MqttBufferWriter(_serverOptions.WriterBufferSize, _serverOptions.WriterBufferSizeMax);
                bufferWriter = new ChannelMqttBufferWriter(writer);
            }
            return bufferWriter;
        }

        public void Return(ChannelMqttBufferWriter bufferWriter)
        {
            if (CanReturn(bufferWriter))
            {
                _bufferWriterQueue.Enqueue(bufferWriter);
            }
        }

        private bool CanReturn(ChannelMqttBufferWriter bufferWriter)
        {
            var options = _poolOptions.CurrentValue;
            if (!options.Enable)
            {
                return false;
            }

            if (bufferWriter.LifeTime < options.PoolingItemMaxLifeTime)
            {
                return true;
            }

            if (options.PoolingLargeBufferSizeItem &&
                bufferWriter.BufferSize > _serverOptions.WriterBufferSize)
            {
                return true;
            }

            return false;
        }

        public IEnumerator<ChannelMqttBufferWriter> GetEnumerator()
        {
            return _bufferWriterQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _bufferWriterQueue.GetEnumerator();
        }


        [DebuggerDisplay("BufferSize = {BufferSize}, LifeTime = {LifeTime}")]
        public sealed class ChannelMqttBufferWriter(MqttBufferWriter bufferWriter)
        {
            private long _tickCount = Environment.TickCount64;
            private readonly MqttBufferWriter _bufferWriter = bufferWriter;

            public int BufferSize => _bufferWriter.GetBuffer().Length;
            public TimeSpan LifeTime => TimeSpan.FromMilliseconds(Environment.TickCount64 - _tickCount);

            public void Reset()
            {
                _tickCount = Environment.TickCount64;
            }

            public static implicit operator MqttBufferWriter(ChannelMqttBufferWriter channelMqttBufferWriter)
            {
                return channelMqttBufferWriter._bufferWriter;
            }
        }
    }
}