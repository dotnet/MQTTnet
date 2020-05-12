using System;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public static class MqttFactoryExtensions
    {
        public static IMqttFactory UseWebSocket4Net(this IMqttFactory mqttFactory)
        {
            if (mqttFactory == null) throw new ArgumentNullException(nameof(mqttFactory));

            return mqttFactory.UseClientAdapterFactory(new WebSocket4NetMqttClientAdapterFactory(mqttFactory.DefaultLogger));
        }
    }
}
