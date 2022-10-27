// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class MqttRpcClient : IMqttRpcClient
    {
        readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
        readonly IMqttClient _mqttClient;
        readonly MqttRpcClientOptions _options;
        
        public MqttRpcClient(IMqttClient mqttClient, MqttRpcClientOptions options)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
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
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));
            
            var topicNames = _options.TopicGenerationStrategy.CreateRpcTopics(new TopicGenerationContext
            {
                MethodName = methodName,
                QualityOfServiceLevel = qualityOfServiceLevel,
                MqttClient = _mqttClient,
                Options = _options
            });

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
#if NET452
                var awaitable = new TaskCompletionSource<byte[]>();
#else
                var awaitable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

                if (!_waitingCalls.TryAdd(responseTopic, awaitable))
                {
                    throw new InvalidOperationException();
                }

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(responseTopic, qualityOfServiceLevel)
                    .Build();

                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
                await _mqttClient.PublishAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                using (cancellationToken.Register(() => { awaitable.TrySetCanceled(); }))
                {
                    return await awaitable.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _waitingCalls.TryRemove(responseTopic, out _);
                
                await _mqttClient.UnsubscribeAsync(responseTopic, cancellationToken).ConfigureAwait(false);
            }
        }

        Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_waitingCalls.TryRemove(eventArgs.ApplicationMessage.Topic, out var awaitable))
            {
                return CompletedTask.Instance;
            }

#if NET452
            Task.Run(() => awaitable.TrySetResult(eventArgs.ApplicationMessage.Payload));
#else
            awaitable.TrySetResult(eventArgs.ApplicationMessage.Payload);
#endif

            // Set this message to handled to that other code can avoid execution etc.
            eventArgs.IsHandled = true;

            return CompletedTask.Instance;
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
    }
}