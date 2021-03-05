using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Extensions.Rpc.Options.TopicGeneration;
using MQTTnet.Protocol;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Subscribing;
using MQTTnet.Implementations;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class MqttRpcClient : IMqttRpcClient
    {
        readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
        readonly IMqttClient _mqttClient;
        readonly IMqttRpcClientOptions _options;
        readonly RpcAwareApplicationMessageReceivedHandler _applicationMessageReceivedHandler;

        [Obsolete("Use MqttRpcClient(IMqttClient mqttClient, IMqttRpcClientOptions options).")]
        public MqttRpcClient(IMqttClient mqttClient) : this(mqttClient, new MqttRpcClientOptions())
        {
        }

        public MqttRpcClient(IMqttClient mqttClient, IMqttRpcClientOptions options)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _applicationMessageReceivedHandler = new RpcAwareApplicationMessageReceivedHandler(
                mqttClient.ApplicationMessageReceivedHandler,
                HandleApplicationMessageReceivedAsync);

            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler;
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

        public async Task<byte[]> ExecuteAsync(string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, CancellationToken cancellationToken)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            if (!(_mqttClient.ApplicationMessageReceivedHandler is RpcAwareApplicationMessageReceivedHandler))
            {
                throw new InvalidOperationException("The application message received handler was modified.");
            }

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

            var requestMessage = new MqttApplicationMessageBuilder()
                .WithTopic(requestTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .Build();

            try
            {
#if NET452
                var promise = new TaskCompletionSource<byte[]>();
#else
                var promise = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
                
                if (!_waitingCalls.TryAdd(responseTopic, promise))
                {
                    throw new InvalidOperationException();
                }

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(responseTopic, qualityOfServiceLevel)
                    .Build();

                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
                await _mqttClient.PublishAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                using (cancellationToken.Register(() => { promise.TrySetCanceled(); }))
                {
                    return await promise.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _waitingCalls.TryRemove(responseTopic, out _);

                await _mqttClient.UnsubscribeAsync(responseTopic).ConfigureAwait(false);
            }
        }

        Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_waitingCalls.TryRemove(eventArgs.ApplicationMessage.Topic, out var promise))
            {
                return PlatformAbstractionLayer.CompletedTask;
            }

#if NET452
            Task.Run(() => promise.TrySetResult(eventArgs.ApplicationMessage.Payload));
#else
            promise.TrySetResult(eventArgs.ApplicationMessage.Payload);
#endif

            // Set this message to handled to that other code can avoid execution etc.
            eventArgs.IsHandled = true;

            return PlatformAbstractionLayer.CompletedTask;
        }

        public void Dispose()
        {
            _mqttClient.ApplicationMessageReceivedHandler = _applicationMessageReceivedHandler.OriginalHandler;

            foreach (var tcs in _waitingCalls)
            {
                tcs.Value.TrySetCanceled();
            }

            _waitingCalls.Clear();
        }
    }
}