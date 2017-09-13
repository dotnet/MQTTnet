using System.Threading.Tasks;

namespace MQTTnet.Core.Client
{
    public static class MqttClientExtensions
    {
        public static Task PublishAsync(this IMqttClient client, params MqttApplicationMessage[] applicationMessages)
        {
            return client.PublishAsync(applicationMessages);
        }
    }
}