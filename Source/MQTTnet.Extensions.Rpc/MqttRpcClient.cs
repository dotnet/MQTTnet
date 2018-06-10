using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc
{
    public class MqttRpcClient : IDisposable
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
        private readonly IMqttClient _mqttClient;

        public MqttRpcClient(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
        }

        public Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return ExecuteAsync(timeout, methodName, Encoding.UTF8.GetBytes(payload), qualityOfServiceLevel, CancellationToken.None);
        }

        public Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, CancellationToken cancellationToken)
        {
            return ExecuteAsync(timeout, methodName, Encoding.UTF8.GetBytes(payload), qualityOfServiceLevel, cancellationToken);
        }

        public Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return ExecuteAsync(timeout, methodName, payload, qualityOfServiceLevel, CancellationToken.None);
        }

        public async Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, CancellationToken cancellationToken)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            if (methodName.Contains("/") || methodName.Contains("+") || methodName.Contains("#"))
            {
                throw new ArgumentException("The method name cannot contain /, + or #.");
            }

            var requestTopic = $"MQTTnet.RPC/{Guid.NewGuid():N}/{methodName}";
            var responseTopic = requestTopic + "/response";

            var requestMessage = new MqttApplicationMessageBuilder()
                .WithTopic(requestTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .Build();

            try
            {
                var tcs = new TaskCompletionSource<byte[]>();
                if (!_waitingCalls.TryAdd(responseTopic, tcs))
                {
                    throw new InvalidOperationException();
                }

                await _mqttClient.SubscribeAsync(responseTopic, qualityOfServiceLevel).ConfigureAwait(false);
                await _mqttClient.PublishAsync(requestMessage).ConfigureAwait(false);
                
                using (var timeoutCts = new CancellationTokenSource(timeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    linkedCts.Token.Register(() =>
                    {
                        if (!tcs.Task.IsCompleted && !tcs.Task.IsFaulted && !tcs.Task.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
                    });

                    try
                    {
                        var result = await tcs.Task.ConfigureAwait(false);
                        timeoutCts.Cancel(false);
                        return result;
                    }
                    catch (TaskCanceledException taskCanceledException)
                    {
                        if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            throw new MqttCommunicationTimedOutException(taskCanceledException);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            finally
            {
                _waitingCalls.TryRemove(responseTopic, out _);
                await _mqttClient.UnsubscribeAsync(responseTopic).ConfigureAwait(false);
            }
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_waitingCalls.TryRemove(eventArgs.ApplicationMessage.Topic, out var tcs))
            {
                return;
            }

            if (tcs.Task.IsCompleted || tcs.Task.IsCanceled)
            {
                return;
            }

            tcs.TrySetResult(eventArgs.ApplicationMessage.Payload);
        }

        public void Dispose()
        {
            foreach (var tcs in _waitingCalls)
            {
                tcs.Value.SetCanceled();
            }

            _waitingCalls.Clear();
        }
    }
}
