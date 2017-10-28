using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscribedTopics = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly MqttServerOptions _options;

        public MqttClientSubscriptionsManager(IOptions<MqttServerOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public MqttClientSubscribeResult Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var responsePacket = subscribePacket.CreateResponse<MqttSubAckPacket>();
            var closeConnection = false;

            lock (_subscribedTopics)
            {
                foreach (var topicFilter in subscribePacket.TopicFilters)
                {
                    var interceptorContext = new MqttSubscriptionInterceptorContext("", topicFilter);
                    _options.SubscriptionsInterceptor?.Invoke(interceptorContext);
                    responsePacket.SubscribeReturnCodes.Add(interceptorContext.AcceptSubscription ? MqttSubscribeReturnCode.SuccessMaximumQoS1 : MqttSubscribeReturnCode.Failure);
                    
                    if (interceptorContext.CloseConnection)
                    {
                        closeConnection = true;
                    }

                    if (interceptorContext.AcceptSubscription)
                    {
                        _subscribedTopics[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
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

            lock (_subscribedTopics)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscribedTopics.Remove(topicFilter);
                }
            }

            return unsubscribePacket.CreateResponse<MqttUnsubAckPacket>();
        }

        public bool IsSubscribed(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            lock (_subscribedTopics)
            {
                foreach (var subscribedTopic in _subscribedTopics)
                {
                    if (publishPacket.QualityOfServiceLevel > subscribedTopic.Value)
                    {
                        continue;
                    }

                    if (!MqttTopicFilterComparer.IsMatch(publishPacket.Topic, subscribedTopic.Key))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
