// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public static class MqttPayloadOwnerFactory
    {
        public static MqttPayloadOwner Rent(int payloadSize, out Memory<byte> payloadMemory)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(payloadSize);
            return PoolPayloadOwner.FromRent(payloadSize, out payloadMemory);
        }

        public static ValueTask<MqttPayloadOwner> JsonSerializeAsync<TValue>(
            TValue value,
            JsonSerializerOptions jsonSerializerOptions = default,
            CancellationToken cancellationToken = default)
        {
            return JsonPayloadOwner.FromSerializeAsync(value, jsonSerializerOptions, cancellationToken);
        }

        public static ValueTask<MqttPayloadOwner> JsonSerializeAsync<TValue>(
           TValue value,
           JsonTypeInfo<TValue> jsonTypeInfo,
           CancellationToken cancellationToken = default)
        {
            return JsonPayloadOwner.FormSerializeAsync(value, jsonTypeInfo, cancellationToken);
        }


        private sealed class PoolPayloadOwner : MqttPayloadOwner
        {
            private readonly byte[] _buffer;
            public override ReadOnlySequence<byte> Payload { get; }

            private PoolPayloadOwner(byte[] buffer, ReadOnlyMemory<byte> payloadMemory)
            {
                _buffer = buffer;
                Payload = new ReadOnlySequence<byte>(payloadMemory);
            }

            public override ValueTask DisposeAsync()
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                return ValueTask.CompletedTask;
            }

            public static MqttPayloadOwner FromRent(int payloadSize, out Memory<byte> payloadMemory)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(payloadSize);
                payloadMemory = buffer.AsMemory(0, payloadSize);
                return new PoolPayloadOwner(buffer, payloadMemory);
            }
        }

        private sealed class JsonPayloadOwner : MqttPayloadOwner
        {
            private readonly Pipe _pipe;
            private static readonly ConcurrentQueue<Pipe> _pipeQueue = new();
            public override ReadOnlySequence<byte> Payload { get; }

            private JsonPayloadOwner(Pipe pipe, ReadOnlySequence<byte> payload)
            {
                _pipe = pipe;
                Payload = payload;
            }

            public override ValueTask DisposeAsync()
            {
                return ReturnPipeAsync(_pipe);
            }

            public static async ValueTask<MqttPayloadOwner> FromSerializeAsync<TValue>(
                TValue value,
                JsonSerializerOptions jsonSerializerOptions = default,
                CancellationToken cancellationToken = default)
            {
                var pipe = RentPipe();

                var stream = pipe.Writer.AsStream(leaveOpen: true);
                await JsonSerializer.SerializeAsync(stream, value, jsonSerializerOptions, cancellationToken);
                await pipe.Writer.CompleteAsync();

                var payload = await ReadAsync(pipe.Reader);
                return new JsonPayloadOwner(pipe, payload);
            }

            public static async ValueTask<MqttPayloadOwner> FormSerializeAsync<TValue>(
                TValue value,
                JsonTypeInfo<TValue> jsonTypeInfo,
                CancellationToken cancellationToken = default)
            {
                var pipe = RentPipe();

                var stream = pipe.Writer.AsStream(leaveOpen: true);
                await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, cancellationToken);
                await pipe.Writer.CompleteAsync();

                var payload = await ReadAsync(pipe.Reader);
                return new JsonPayloadOwner(pipe, payload);
            }


            private static async ValueTask<ReadOnlySequence<byte>> ReadAsync(
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

            private static Pipe RentPipe()
            {
                if (!_pipeQueue.TryDequeue(out var pipe))
                {
                    pipe = new Pipe();
                }
                return pipe;
            }

            private static async ValueTask ReturnPipeAsync(Pipe pipe)
            {
                await pipe.Reader.CompleteAsync();
                pipe.Reset();
                _pipeQueue.Enqueue(pipe);
            }
        }
    }
}
