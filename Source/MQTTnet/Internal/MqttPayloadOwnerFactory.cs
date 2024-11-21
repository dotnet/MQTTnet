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
        /// <param name="payloadMemory"></param>
        /// <returns></returns>
        public static MqttPayloadOwner CreateSingleSegment(int payloadSize, out Memory<byte> payloadMemory)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(payloadSize);
            return SingleSegmentPayloadOwner.Create(payloadSize, out payloadMemory);
        }

        /// <summary>
        /// Create owner for a multiple segments payload
        /// </summary>
        /// <param name="payloadFactory"></param>
        /// <returns></returns>
        public static ValueTask<MqttPayloadOwner> CreateMultipleSegmentAsync(Func<PipeWriter, ValueTask> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(payloadFactory);
            return MultipleSegmentPayloadOwner.CreateAsync(payloadFactory);
        }

        private sealed class SingleSegmentPayloadOwner : MqttPayloadOwner
        {
            private readonly byte[] _buffer;
            public override ReadOnlySequence<byte> Payload { get; }

            private SingleSegmentPayloadOwner(byte[] buffer, ReadOnlyMemory<byte> payloadMemory)
            {
                _buffer = buffer;
                Payload = new ReadOnlySequence<byte>(payloadMemory);
            }

            public static MqttPayloadOwner Create(int payloadSize, out Memory<byte> payloadMemory)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(payloadSize);
                payloadMemory = buffer.AsMemory(0, payloadSize);
                return new SingleSegmentPayloadOwner(buffer, payloadMemory);
            }

            protected override ValueTask DisposeAsync(bool disposing)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
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

            public static async ValueTask<MqttPayloadOwner> CreateAsync(Func<PipeWriter, ValueTask> payloadFactory)
            {
                if (!_pipeQueue.TryDequeue(out var pipe))
                {
                    pipe = new Pipe();
                }

                await payloadFactory.Invoke(pipe.Writer);
                await pipe.Writer.CompleteAsync();

                var payload = await ReadPayloadAsync(pipe.Reader);
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
                CancellationToken cancellationToken = default)
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
