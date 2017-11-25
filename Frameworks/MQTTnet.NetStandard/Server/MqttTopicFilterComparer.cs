using System;

namespace MQTTnet.Server
{
    public static class MqttTopicFilterComparer
    {
        private static readonly char[] TopicLevelSeparator = { '/' };

        public static bool IsMatch(string topic, string filter)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            if (string.Equals(topic, filter, StringComparison.Ordinal))
            {
                return true;
            }

            var fragmentsTopic = topic.Split(TopicLevelSeparator, StringSplitOptions.None);
            var fragmentsFilter = filter.Split(TopicLevelSeparator, StringSplitOptions.None);

            for (var i = 0; i < fragmentsFilter.Length; i++)
            {
                switch (fragmentsFilter[i])
                {
                    case "+": continue;
                    case "#" when i == fragmentsFilter.Length - 1: return true;
                }

                if (i >= fragmentsTopic.Length)
                {
                    return false;
                }

                if (!string.Equals(fragmentsFilter[i], fragmentsTopic[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return fragmentsTopic.Length <= fragmentsFilter.Length;
        }
    }
}
