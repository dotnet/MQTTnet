// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public static class MqttPayloadOwnerFactory
    {
        /// <summary>
        /// Create owner for a single segment payload
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="payloadFactory"></param>
        /// <returns></returns>
        public static MqttPayloadOwner CreateSingleSegment(int payloadSize, Action<Memory<byte>> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(payloadFactory);
            return SingleSegmentPayloadOwner.Create(payloadSize, payloadFactory);
        }

        /// <summary>
        /// Create owner for a multiple segments payload
        /// </summary>
        /// <param name="payloadFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static ValueTask<MqttPayloadOwner> CreateMultipleSegmentAsync(Func<PipeWriter, ValueTask> payloadFactory, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payloadFactory);
            return MultipleSegmentPayloadOwner.CreateAsync(payloadFactory, cancellationToken);
        }

        private sealed class SingleSegmentPayloadOwner : MqttPayloadOwner
        {
            private readonly byte[] _buffer;
            public override ReadOnlySequence<byte> Payload { get; }

            private SingleSegmentPayloadOwner(byte[] buffer, ReadOnlyMemory<byte> payload)
            {
                _buffer = buffer;
                Payload = new ReadOnlySequence<byte>(payload);
            }

            public static MqttPayloadOwner Create(int payloadSize, Action<Memory<byte>> payloadFactory)
            {
                byte[] buffer;
                Memory<byte> payload;

                if (payloadSize <= 0)
                {
                    buffer = Array.Empty<byte>();
                    payload = Memory<byte>.Empty;
                }
                else
                {
                    buffer = ArrayPool<byte>.Shared.Rent(payloadSize);
                    payload = buffer.AsMemory(0, payloadSize);
                }

                payloadFactory.Invoke(payload);
                return new SingleSegmentPayloadOwner(buffer, payload);
            }

            protected override ValueTask DisposeAsync(bool disposing)
            {
                if (_buffer.Length > 0)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                }
                return ValueTask.CompletedTask;
            }
        }

        private sealed class MultipleSegmentPayloadOwner : MqttPayloadOwner
        {
            private readonly Pipe _pipe;
            private static readonly ConcurrentQueue<Pipe> _pipeQueue = new();
            public override ReadOnlySequence<byte> Payload { get; }

            private MultipleSegmentPayloadOwner(Pipe pipe, ReadOnlySequence<byte> payload)
            {
                _pipe = pipe;
                Payload = payload;
            }

            public static async ValueTask<MqttPayloadOwner> CreateAsync(Func<PipeWriter, ValueTask> payloadFactory, CancellationToken cancellationToken)
            {
                if (!_pipeQueue.TryDequeue(out var pipe))
                {
                    pipe = new Pipe();
                }

                await payloadFactory.Invoke(pipe.Writer);
                await pipe.Writer.CompleteAsync();

                var payload = await ReadPayloadAsync(pipe.Reader, cancellationToken);
                return new MultipleSegmentPayloadOwner(pipe, payload);
            }

            protected override async ValueTask DisposeAsync(bool disposing)
            {
                await _pipe.Reader.CompleteAsync();
                _pipe.Reset();
                _pipeQueue.Enqueue(_pipe);
            }

            private static async ValueTask<ReadOnlySequence<byte>> ReadPayloadAsync(
                PipeReader pipeReader,
                CancellationToken cancellationToken)
            {
                var readResult = await pipeReader.ReadAsync(cancellationToken);
                while (!readResult.IsCompleted)
                {
                    pipeReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.Start);
                    readResult = await pipeReader.ReadAsync(cancellationToken);
                }

                return readResult.Buffer;
            }
        }
    }
}
