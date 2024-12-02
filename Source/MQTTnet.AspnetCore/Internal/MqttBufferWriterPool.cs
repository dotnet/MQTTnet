// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;

namespace MQTTnet.AspNetCore
{
    sealed class MqttBufferWriterPool(MqttServerOptions serverOptions)
    {
        private readonly MqttServerOptions _serverOptions = serverOptions;
        private readonly ConcurrentQueue<RecyclableMqttBufferWriter> _queue = new();

        public RecyclableMqttBufferWriter Rent()
        {
            if (_queue.TryDequeue(out var bufferWriter))
            {
                bufferWriter.Reset();
            }
            else
            {
                var writer = new MqttBufferWriter(_serverOptions.WriterBufferSize, _serverOptions.WriterBufferSizeMax);
                bufferWriter = new RecyclableMqttBufferWriter(writer);
            }
            return bufferWriter;
        }

        public void Return(RecyclableMqttBufferWriter bufferWriter)
        {
            if (bufferWriter.CanRecycle)
            {
                _queue.Enqueue(bufferWriter);
            }
        }


        public sealed class RecyclableMqttBufferWriter(MqttBufferWriter bufferWriter)
        {
            private long _tickCount = Environment.TickCount64;
            private readonly MqttBufferWriter _bufferWriter = bufferWriter;
            private static readonly TimeSpan _maxLifeTime = TimeSpan.FromMinutes(1d);

            /// <summary>
            /// We only recycle the MqttBufferWriter created by channels that are frequently offline.
            /// This ensures that the MqttBufferWriter cache hit rate is high and does not cause the problem of too many MqttBufferWriters being pooled when the number of channels is reduced.
            /// </summary>
            /// <returns></returns>
            public bool CanRecycle => TimeSpan.FromMilliseconds(Environment.TickCount64 - _tickCount) < _maxLifeTime;

            public void Reset()
            {
                _tickCount = Environment.TickCount64;
            }

            public static implicit operator MqttBufferWriter(RecyclableMqttBufferWriter bufferWriterItem)
            {
                return bufferWriterItem._bufferWriter;
            }
        }
    }
}