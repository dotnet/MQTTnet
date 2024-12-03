// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;

namespace MQTTnet.AspNetCore
{
    sealed class MqttBufferWriterPool
    {
        private readonly MqttServerOptions _serverOptions;
        private readonly IOptionsMonitor<MqttBufferWriterPoolOptions> _poolOptions;
        private readonly ConcurrentQueue<ResettableMqttBufferWriter> _bufferWriterQueue = new();

        public MqttBufferWriterPool(
            MqttServerOptions serverOptions,
            IOptionsMonitor<MqttBufferWriterPoolOptions> poolOptions)
        {
            _serverOptions = serverOptions;
            _poolOptions = poolOptions;
        }

        public ResettableMqttBufferWriter Rent()
        {
            if (_bufferWriterQueue.TryDequeue(out var bufferWriter))
            {
                bufferWriter.Reset();
            }
            else
            {
                var writer = new MqttBufferWriter(_serverOptions.WriterBufferSize, _serverOptions.WriterBufferSizeMax);
                bufferWriter = new ResettableMqttBufferWriter(writer);
            }
            return bufferWriter;
        }

        public void Return(ResettableMqttBufferWriter bufferWriter)
        {
            var options = _poolOptions.CurrentValue;
            if (options.Enable && bufferWriter.LifeTime < options.MaxLifeTime)
            {
                _bufferWriterQueue.Enqueue(bufferWriter);
            }
        }


        public sealed class ResettableMqttBufferWriter(MqttBufferWriter bufferWriter)
        {
            private long _tickCount = Environment.TickCount64;
            private readonly MqttBufferWriter _bufferWriter = bufferWriter;

            public TimeSpan LifeTime => TimeSpan.FromMilliseconds(Environment.TickCount64 - _tickCount);

            public void Reset()
            {
                _tickCount = Environment.TickCount64;
            }

            public static implicit operator MqttBufferWriter(ResettableMqttBufferWriter resettableMqttBufferWriter)
            {
                return resettableMqttBufferWriter._bufferWriter;
            }
        }
    }
}