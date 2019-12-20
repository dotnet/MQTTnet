using MQTTnet.Protocol;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public class MqttSubscriptionIndex : Dictionary<string, MqttSubscriptionNode>
    {
        public MqttSubscriptionIndex()
        {
        }

        public MqttSubscriptionNode SingleLevelWildcard { get; set; }

        public MqttSubscriptionNode MultiLevelWildCard { get; set; }

        public static void Subscribe(TopicFilter filter, MqttSubscriptionIndex items)
        {
            var segments = filter.Topic.Split(MqttTopicFilterComparer.LevelSeparator);
            var currentItems = items;

            for (int i = 0; i < segments.Length; i++)
            {
                var node = Subscribe(segments[i], currentItems);
                if (i == segments.Length - 1)
                {
                    node.TopicFilter = filter;
                }
                else
                {
                    if (node.IsMultiLevelWildcard)
                    {
                        break;
                    }
                    currentItems = node.Children;
                }
            }
        }

        private static MqttSubscriptionNode Subscribe(string topicSegment, MqttSubscriptionIndex items)
        {
            if (topicSegment.Length == 1)
            {
                switch (topicSegment[0])
                {
                    case MqttTopicFilterComparer.SingleLevelWildcard:
                        if (items.SingleLevelWildcard == null)
                        {
                            items.SingleLevelWildcard = new MqttSubscriptionNode(new string(MqttTopicFilterComparer.SingleLevelWildcard, 1));
                        }

                        return items.SingleLevelWildcard;
                    case MqttTopicFilterComparer.MultiLevelWildcard:
                        if (items.MultiLevelWildCard == null)
                        {
                            items.MultiLevelWildCard = new MqttSubscriptionNode(new string(MqttTopicFilterComparer.MultiLevelWildcard, 1));
                        }
                        return items.MultiLevelWildCard;
                    default:
                        break;
                }
            }


            if (!items.TryGetValue(topicSegment, out var node))
            {
                node = new MqttSubscriptionNode(topicSegment);
                items.Add(topicSegment, node);
            }

            return node;
        }

        public HashSet<MqttQualityOfServiceLevel> GetQosLevels(string topic)
        {
            var topicSegments = topic.Split(MqttTopicFilterComparer.LevelSeparator);
            var result = new HashSet<MqttQualityOfServiceLevel>();
            GetQosLevels(topicSegments, 0, this, result);
            return result;
        }

        private static void GetQosLevels(string[] topicSegments, int level, MqttSubscriptionIndex items, HashSet<MqttQualityOfServiceLevel> result)
        {
            if (items.MultiLevelWildCard != null)
            {
                result.Add(items.MultiLevelWildCard.TopicFilter.QualityOfServiceLevel);
            }

            if (items.SingleLevelWildcard != null)
            {
                if (level == topicSegments.Length - 1)
                {
                    result.Add(items.SingleLevelWildcard.TopicFilter.QualityOfServiceLevel);
                    return;
                }
                GetQosLevels(topicSegments, level + 1, items.SingleLevelWildcard.Children, result);
            }
            
            var segment = topicSegments[level];
            if (items.TryGetValue(segment, out var node))
            {
                if (level == topicSegments.Length - 1)
                {
                    result.Add(node.TopicFilter.QualityOfServiceLevel);
                    return;
                }

                GetQosLevels(topicSegments, level + 1, node.Children, result);
            }
        }
    }
}

