using System;

namespace MQTTnet.Core.Server
{
    public static class MqttTopicFilterComparer
    {
        private const char TopicLevelSeparator = '/';

        public static bool IsMatch(string topic, string filter)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (string.Equals(topic, filter, StringComparison.Ordinal))
            {
                return true;
            }

            var fragmentsTopic = topic.Split(new[] { TopicLevelSeparator }, StringSplitOptions.None);
            var fragmentsFilter = filter.Split(new[] { TopicLevelSeparator }, StringSplitOptions.None);

            for (var i = 0; i < fragmentsFilter.Length; i++)
            {
                if (fragmentsFilter[i] == "+")
                {
                    continue;
                }

                if (fragmentsFilter[i] == "#" && i == fragmentsFilter.Length - 1)
                {
                    return true;
                }

                if (i >= fragmentsTopic.Length)
                {
                    return false;
                }

                if (!string.Equals(fragmentsFilter[i], fragmentsTopic[i]))
                {
                    return false;
                }
            }

            if (fragmentsTopic.Length > fragmentsFilter.Length)
            {
                return false;
            }

            return true;
        }
    }
}
