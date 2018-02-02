using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly IMqttServerOptions _options;
        private readonly string _clientId;
        
        public MqttClientSubscriptionsManager(IMqttServerOptions options, string clientId)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public Action<string, TopicFilter> TopicSubscribedCallback { get; set; }
        public Action<string, string> TopicUnsubscribedCallback { get; set; }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var result = new MqttClientSubscribeResult
            {
                ResponsePacket = subscribePacket.CreateResponse<MqttSubAckPacket>(),
                CloseConnection = false
            };

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var topicFilter in subscribePacket.TopicFilters)
                {
                    var interceptorContext = InterceptSubscribe(topicFilter);
                    if (!interceptorContext.AcceptSubscription)
                    {
                        result.ResponsePacket.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    }
                    else
                    {
                        result.ResponsePacket.SubscribeReturnCodes.Add(ConvertToMaximumQoS(topicFilter.QualityOfServiceLevel));
                    }

                    if (interceptorContext.CloseConnection)
                    {
                        result.CloseConnection = true;
                    }

                    if (interceptorContext.AcceptSubscription)
                    {
                        _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                        TopicSubscribedCallback?.Invoke(_clientId, topicFilter);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        public async Task<MqttUnsubAckPacket> UnsubscribeAsync(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscriptions.Remove(topicFilter);
                    TopicUnsubscribedCallback?.Invoke(_clientId, topicFilter);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return unsubscribePacket.CreateResponse<MqttUnsubAckPacket>();
        }

        public async Task<CheckSubscriptionsResult> CheckSubscriptionsAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var qosLevels = new HashSet<MqttQualityOfServiceLevel>();
                foreach (var subscription in _subscriptions)
                {
                    if (!MqttTopicFilterComparer.IsMatch(applicationMessage.Topic, subscription.Key))
                    {
                        continue;
                    }

                    qosLevels.Add(subscription.Value);
                }

                if (qosLevels.Count == 0)
                {
                    return new CheckSubscriptionsResult
                    {
                        IsSubscribed = false
                    };
                }

                return CreateSubscriptionResult(applicationMessage, qosLevels);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        private static MqttSubscribeReturnCode ConvertToMaximumQoS(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS2;
                default: return MqttSubscribeReturnCode.Failure;
            }
        }

        private MqttSubscriptionInterceptorContext InterceptSubscribe(TopicFilter topicFilter)
        {
            var interceptorContext = new MqttSubscriptionInterceptorContext(_clientId, topicFilter);
            _options.SubscriptionInterceptor?.Invoke(interceptorContext);
            return interceptorContext;
        }

        private static CheckSubscriptionsResult CreateSubscriptionResult(MqttApplicationMessage applicationMessage, HashSet<MqttQualityOfServiceLevel> subscribedQoSLevels)
        {
            MqttQualityOfServiceLevel effectiveQoS;
            if (subscribedQoSLevels.Contains(applicationMessage.QualityOfServiceLevel))
            {
                effectiveQoS = applicationMessage.QualityOfServiceLevel;
            }
            else if (subscribedQoSLevels.Count == 1)
            {
                effectiveQoS = subscribedQoSLevels.First();
            }
            else
            {
                effectiveQoS = subscribedQoSLevels.Max();
            }

            return new CheckSubscriptionsResult
            {
                IsSubscribed = true,
                QualityOfServiceLevel = effectiveQoS
            };
        }
    }
}
