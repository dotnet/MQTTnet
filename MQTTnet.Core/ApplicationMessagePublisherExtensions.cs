using System;
using System.Threading.Tasks;

namespace MQTTnet.Core
{
    public static class ApplicationMessagePublisherExtensions
    {
        public static Task PublishAsync(this IApplicationMessagePublisher client, params MqttApplicationMessage[] applicationMessages)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            return client.PublishAsync(applicationMessages);
        }
    }
}
