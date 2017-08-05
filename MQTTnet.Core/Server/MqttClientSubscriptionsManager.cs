using System;
using System.Collections.Generic;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscribedTopics = new Dictionary<string, MqttQualityOfServiceLevel>();

        public MqttSubAckPacket Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var responsePacket = subscribePacket.CreateResponse<MqttSubAckPacket>();

            lock (_subscribedTopics)
            {
                foreach (var topicFilter in subscribePacket.TopicFilters)
                {
                    _subscribedTopics[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    responsePacket.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS1); // TODO: Add support for QoS 2.
                }
            }

            return responsePacket;
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
