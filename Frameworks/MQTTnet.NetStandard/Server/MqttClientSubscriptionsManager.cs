using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly MqttServerOptions _options;

        public MqttClientSubscriptionsManager(MqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public MqttClientSubscribeResult Subscribe(MqttSubscribePacket subscribePacket, string clientId)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var responsePacket = subscribePacket.CreateResponse<MqttSubAckPacket>();
            var closeConnection = false;

            lock (_subscriptions)
            {
                foreach (var topicFilter in subscribePacket.TopicFilters)
                {
                    var interceptorContext = new MqttSubscriptionInterceptorContext(clientId, topicFilter);
                    _options.SubscriptionInterceptor?.Invoke(interceptorContext);
                    responsePacket.SubscribeReturnCodes.Add(interceptorContext.AcceptSubscription ? MqttSubscribeReturnCode.SuccessMaximumQoS1 : MqttSubscribeReturnCode.Failure);
                    
                    if (interceptorContext.CloseConnection)
                    {
                        closeConnection = true;
                    }

                    if (interceptorContext.AcceptSubscription)
                    {
                        _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    }
                }
            }

            return new MqttClientSubscribeResult
            {
                ResponsePacket = responsePacket,
                CloseConnection = closeConnection
            };
        }

        public MqttUnsubAckPacket Unsubscribe(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            lock (_subscriptions)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscriptions.Remove(topicFilter);
                }
            }

            return unsubscribePacket.CreateResponse<MqttUnsubAckPacket>();
        }

        public CheckSubscriptionsResult CheckSubscriptions(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    if (!MqttTopicFilterComparer.IsMatch(publishPacket.Topic, subscription.Key))
                    {
                        continue;
                    }

                    var effectiveQos = subscription.Value;
                    if (publishPacket.QualityOfServiceLevel < effectiveQos)
                    {
                        effectiveQos = publishPacket.QualityOfServiceLevel;
                    }

                    return new CheckSubscriptionsResult
                    {
                        IsSubscribed = true,
                        QualityOfServiceLevel = effectiveQos
                    };
                }
            }

            return new CheckSubscriptionsResult
            {
                IsSubscribed = false
            };
        }
    }
}
