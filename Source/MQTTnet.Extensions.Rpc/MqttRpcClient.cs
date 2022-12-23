// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class MqttRpcClient : IMqttRpcClient
    {
        readonly IMqttClient _mqttClient;
        readonly MqttRpcClientOptions _options;

        readonly ConcurrentDictionary<string, AsyncTaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, AsyncTaskCompletionSource<byte[]>>();

        public MqttRpcClient(IMqttClient mqttClient, MqttRpcClientOptions options)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        }

        public void Dispose()
        {
            _mqttClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;

            foreach (var tcs in _waitingCalls)
            {
                tcs.Value.TrySetCanceled();
            }

            _waitingCalls.Clear();
        }

        public async Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                try
                {
                    return await ExecuteAsync(methodName, payload, qualityOfServiceLevel, timeoutToken.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    if (timeoutToken.IsCancellationRequested)
                    {
                        throw new MqttCommunicationTimedOutException(exception);
                    }

                    throw;
                }
            }
        }

        public async Task<byte[]> ExecuteAsync(string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, CancellationToken cancellationToken = default)
        {
            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            var context = new TopicGenerationContext(_mqttClient, _options, methodName, qualityOfServiceLevel);
            var topicNames = _options.TopicGenerationStrategy.CreateRpcTopics(context);

            var requestTopic = topicNames.RequestTopic;
            var responseTopic = topicNames.ResponseTopic;

            if (string.IsNullOrWhiteSpace(requestTopic))
            {
                throw new MqttProtocolViolationException("RPC request topic is empty.");
            }

            if (string.IsNullOrWhiteSpace(responseTopic))
            {
                throw new MqttProtocolViolationException("RPC response topic is empty.");
            }

            var requestMessageBuilder = new MqttApplicationMessageBuilder().WithTopic(requestTopic).WithPayload(payload).WithQualityOfServiceLevel(qualityOfServiceLevel);

            if (_mqttClient.Options.ProtocolVersion == MqttProtocolVersion.V500)
            {
                requestMessageBuilder.WithResponseTopic(responseTopic);
            }

            var requestMessage = requestMessageBuilder.Build();

            try
            {
                var awaitable = new AsyncTaskCompletionSource<byte[]>();

                if (!_waitingCalls.TryAdd(responseTopic, awaitable))
                {
                    throw new InvalidOperationException();
                }

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(responseTopic, qualityOfServiceLevel).Build();

                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
                await _mqttClient.PublishAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                using (cancellationToken.Register(
                           () =>
                           {
                               awaitable.TrySetCanceled();
                           }))
                {
                    return await awaitable.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _waitingCalls.TryRemove(responseTopic, out _);

                await _mqttClient.UnsubscribeAsync(responseTopic, CancellationToken.None).ConfigureAwait(false);
            }
        }

        Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_waitingCalls.TryRemove(eventArgs.ApplicationMessage.Topic, out var awaitable))
            {
                return CompletedTask.Instance;
            }

            awaitable.TrySetResult(eventArgs.ApplicationMessage.Payload);

            // Set this message to handled to that other code can avoid execution etc.
            eventArgs.IsHandled = true;

            return CompletedTask.Instance;
        }
    }
}