using System;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public static class ApplicationMessagePublisherExtensions
    {
        public static Task PublishAsync(this IApplicationMessagePublisher publisher, params MqttApplicationMessage[] applicationMessages)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            return publisher.PublishAsync(applicationMessages);
        }

        public static Task PublishAsync(this IApplicationMessagePublisher publisher, string topic)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            
            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
            );
        }

        public static Task PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
            );
        }

        public static Task PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
            );
        }

        public static Task PublishAsync(this IApplicationMessagePublisher publisher, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder)
        {
            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return publisher.PublishAsync(message);
        }
    }
}
