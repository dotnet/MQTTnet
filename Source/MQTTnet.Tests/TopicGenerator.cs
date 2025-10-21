using MQTTnet.Extensions.TopicTemplate;

namespace MQTTnet.Tests;

public class TopicGenerator
{
    public static void Generate(
        int numPublishers, int numTopicsPerPublisher,
        out Dictionary<string, List<string>> topicsByPublisher,
        out Dictionary<string, List<string>> singleWildcardTopicsByPublisher,
        out Dictionary<string, List<string>> multiWildcardTopicsByPublisher
    )
    {
        topicsByPublisher = new Dictionary<string, List<string>>();
        singleWildcardTopicsByPublisher = new Dictionary<string, List<string>>();
        multiWildcardTopicsByPublisher = new Dictionary<string, List<string>>();

        // Find some reasonable distribution across three topic levels
        var topicsPerLevel = (int)Math.Pow(numTopicsPerPublisher, (1.0 / 3.0));
        if (topicsPerLevel <= 0)
        {
            topicsPerLevel = 1;
        }

        int numLevel1Topics = topicsPerLevel;
        int numLevel2Topics = topicsPerLevel;

        var maxNumLevel3Topics = 1 + (int)((double)numTopicsPerPublisher / numLevel1Topics / numLevel2Topics);
        if (maxNumLevel3Topics <= 0)
        {
            maxNumLevel3Topics = 1;
        }

        MqttTopicTemplate baseTemplate = new MqttTopicTemplate("{publisher}/{building}/{level}/{sensor}");

        for (var p = 0; p < numPublishers; ++p)
        {
            int publisherTopicCount = 0;
            var publisherName = "pub" + p;
            var publisherTemplate = baseTemplate
                .WithParameter("publisher", publisherName);

            for (var l1 = 0; l1 < numLevel1Topics; ++l1)
            {
                var l1Template = publisherTemplate.WithParameter("building", "building" + (l1 + 1));
                for (var l2 = 0; l2 < numLevel2Topics; ++l2)
                {
                    var l2Template = l1Template.WithParameter("level", "level" + (l2 + 1));
                    for (var l3 = 0; l3 < maxNumLevel3Topics; ++l3)
                    {
                        if (publisherTopicCount >= numTopicsPerPublisher)
                            break;

                        var l3Template = l2Template
                            .WithParameter("sensor", "sensor" + (l3 + 1));
                        AddPublisherTopic(publisherName, l3Template.TopicFilter, topicsByPublisher);

                        if (l2 == 0)
                        {
                            var singleWildcardTopic = l1Template
                                .WithParameter("sensor", "sensor" + (l3 + 1))
                                .TopicFilter;
                            AddPublisherTopic(publisherName, singleWildcardTopic, singleWildcardTopicsByPublisher);
                        }
                        if ((l1 == 0) && (l3 == 0))
                        {
                            var multiWildcardTopic = publisherTemplate
                                .WithParameter("level", "level" + (l2 + 1))
                                .TopicFilter;
                            AddPublisherTopic(publisherName, multiWildcardTopic, multiWildcardTopicsByPublisher);
                        }

                        ++publisherTopicCount;
                    }
                }
            }
        }
    }

    static void AddPublisherTopic(string publisherName, string topic, Dictionary<string, List<string>> topicsByPublisher)
    {
        List<string> topicList;
        if (!topicsByPublisher.TryGetValue(publisherName, out topicList))
        {
            topicList = new List<string>();
            topicsByPublisher.Add(publisherName, topicList);
        }
        topicList.Add(topic);
    }
}