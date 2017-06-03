using System;
using System.Collections.Concurrent;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        private readonly ConcurrentDictionary<string, MqttQualityOfServiceLevel> _subscribedTopics = new ConcurrentDictionary<string, MqttQualityOfServiceLevel>();

        public MqttSubAckPacket Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var responsePacket = subscribePacket.CreateResponse<MqttSubAckPacket>();
            foreach (var topicFilter in subscribePacket.TopicFilters)
            {
                _subscribedTopics[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                responsePacket.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS1); // TODO: Add support for QoS 2.
            }

            return responsePacket;
        }

        public MqttUnsubAckPacket Unsubscribe(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                MqttQualityOfServiceLevel _;
                _subscribedTopics.TryRemove(topicFilter, out _);
            }

            return unsubscribePacket.CreateResponse<MqttUnsubAckPacket>();
        }

        public bool IsTopicSubscribed(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            foreach (var subscribedTopic in _subscribedTopics)
            {
                if (!MqttTopicFilterComparer.IsMatch(publishPacket.Topic, subscribedTopic.Key))
                {
                    continue;
                }

                if (subscribedTopic.Value < publishPacket.QualityOfServiceLevel)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
