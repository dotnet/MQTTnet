using System;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public static class MqttFactoryExtensions
    {
        public static MqttFactory UseWebSocket4Net(this MqttFactory mqttFactory)
        {
            if (mqttFactory == null) throw new ArgumentNullException(nameof(mqttFactory));

            return mqttFactory.UseClientAdapterFactory(new WebSocket4NetMqttClientAdapterFactory());
        }
    }
}
