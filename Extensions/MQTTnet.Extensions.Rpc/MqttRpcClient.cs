using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class MqttRpcClient : IDisposable
    {
        private const string ResponseTopic = "$RPC/+/+/response";
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
        private readonly IMqttClient _mqttClient;
        private bool _isEnabled;

        public MqttRpcClient(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
        }

        public async Task EnableAsync()
        {
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(ResponseTopic).WithAtLeastOnceQoS().Build());
            _isEnabled = true;
        }

        public async Task DisableAsync()
        {
            await _mqttClient.UnsubscribeAsync(ResponseTopic);
            _isEnabled = false;
        }

        public async Task<byte[]> ExecuteAsync(TimeSpan timeout, string methodName, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            if (methodName.Contains("/") || methodName.Contains("+") || methodName.Contains("#"))
            {
                throw new ArgumentException("The method name cannot contain /, + or #.");
            }

            if (!_isEnabled)
            {
                throw new InvalidOperationException("The RPC client is not enabled.");
            }

            var requestTopic = $"$MQTTnet.RPC/{Guid.NewGuid():N}/{methodName}";
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

                await _mqttClient.PublishAsync(requestMessage);
                return await tcs.Task.TimeoutAfter(timeout);
            }
            finally
            {
                _waitingCalls.TryRemove(responseTopic, out _);
            }
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_waitingCalls.TryRemove(eventArgs.ApplicationMessage.Topic, out TaskCompletionSource<byte[]> tcs))
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
