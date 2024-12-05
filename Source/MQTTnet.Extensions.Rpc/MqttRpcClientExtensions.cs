// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Rpc
{
    public static class MqttRpcClientExtensions
    {
        [Obsolete("Use the method ExecuteTimeoutAsync instead.")]
        public static async Task<byte[]> ExecuteAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null)
        {
            var response = await client.ExecuteTimeoutAsync(timeout, methodName, payload, qualityOfServiceLevel, parameters).ConfigureAwait(false);
            return response.ToArray();
        }

        [Obsolete("Use the method ExecuteTimeoutAsync instead.")]
        public static async Task<byte[]> ExecuteAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, ReadOnlyMemory<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null)
        {
            var response = await client.ExecuteTimeoutAsync(timeout, methodName, payload, qualityOfServiceLevel, parameters).ConfigureAwait(false);
            return response.ToArray();
        }

        public static Task<ReadOnlySequence<byte>> ExecuteTimeoutAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return MqttTimeoutAsync(timeout, cancellationToken, linkedCancellationToken
                => client.ExecuteAsync(methodName, payload, qualityOfServiceLevel, parameters, linkedCancellationToken));
        }

        public static Task<ReadOnlySequence<byte>> ExecuteTimeoutAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, Stream payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return MqttTimeoutAsync(timeout, cancellationToken, linkedCancellationToken
                => client.ExecuteAsync(methodName, payload, qualityOfServiceLevel, parameters, linkedCancellationToken));
        }

        public static Task<ReadOnlySequence<byte>> ExecuteTimeoutAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, ReadOnlyMemory<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return MqttTimeoutAsync(timeout, cancellationToken, linkedCancellationToken
                => client.ExecuteAsync(methodName, payload, qualityOfServiceLevel, parameters, linkedCancellationToken));
        }

        public static Task<ReadOnlySequence<byte>> ExecuteTimeoutAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return MqttTimeoutAsync(timeout, cancellationToken, linkedCancellationToken
                => client.ExecuteAsync(methodName, payload, qualityOfServiceLevel, parameters, linkedCancellationToken));
        }

        public static Task<ReadOnlySequence<byte>> ExecuteTimeoutAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, Func<PipeWriter, ValueTask> payloadFactory, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return MqttTimeoutAsync(timeout, cancellationToken, linkedCancellationToken
                => client.ExecuteAsync(methodName, payloadFactory, qualityOfServiceLevel, parameters, linkedCancellationToken));
        }

        private static async Task<T> MqttTimeoutAsync<T>(TimeSpan timeout, CancellationToken cancellationToken, Func<CancellationToken, Task<T>> executor)
        {
            using var timeoutTokenSource = new CancellationTokenSource(timeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            try
            {
                return await executor(linkedTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (timeoutTokenSource.IsCancellationRequested)
            {
                throw new MqttCommunicationTimedOutException(exception);
            }
        }

        public static Task<ReadOnlySequence<byte>> ExecuteAsync(this IMqttRpcClient client, string methodName, ReadOnlyMemory<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return client.ExecuteAsync(methodName, new ReadOnlySequence<byte>(payload), qualityOfServiceLevel, parameters, cancellationToken);
        }

        public static Task<ReadOnlySequence<byte>> ExecuteAsync(this IMqttRpcClient client, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            return string.IsNullOrEmpty(payload)
                ? client.ExecuteAsync(methodName, ReadOnlySequence<byte>.Empty, qualityOfServiceLevel, parameters, cancellationToken)
                : client.ExecuteAsync(methodName, WritePayloadAsync, qualityOfServiceLevel, parameters, cancellationToken);

            async ValueTask WritePayloadAsync(PipeWriter writer)
            {
                Encoding.UTF8.GetBytes(payload, writer);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<ReadOnlySequence<byte>> ExecuteAsync(this IMqttRpcClient client, string methodName, Stream payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return client.ExecuteAsync(methodName, WritePayloadAsync, qualityOfServiceLevel, parameters, cancellationToken);

            async ValueTask WritePayloadAsync(PipeWriter writer)
            {
                await payload.CopyToAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<ReadOnlySequence<byte>> ExecuteAsync(this IMqttRpcClient client, string methodName, Func<PipeWriter, ValueTask> payloadFactory, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(client);
            await using var payloadOwner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(payloadFactory, cancellationToken).ConfigureAwait(false);
            return await client.ExecuteAsync(methodName, payloadOwner.Payload, qualityOfServiceLevel, parameters, cancellationToken).ConfigureAwait(false);
        }
    }
}