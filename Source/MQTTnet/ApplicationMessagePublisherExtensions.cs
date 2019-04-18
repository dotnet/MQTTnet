using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public static class ApplicationMessagePublisherExtensions
    {
        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, MqttApplicationMessage applicationMessage)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return publisher.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public static async Task PublishAsync(this IApplicationMessagePublisher publisher, IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await publisher.PublishAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic, IEnumerable<byte> payload)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithRetainFlag(retain)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, bool retain)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build());
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder, CancellationToken cancellationToken)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return publisher.PublishAsync(message, cancellationToken);
        }

        public static ValueTask<MqttClientPublishReasonCode> PublishAsync(this IApplicationMessagePublisher publisher, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return publisher.PublishAsync(message, CancellationToken.None);
        }
    }
}