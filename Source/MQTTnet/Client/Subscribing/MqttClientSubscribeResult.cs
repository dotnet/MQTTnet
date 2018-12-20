using System.Collections.Generic;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeResult
    {
        public List<MqttClientSubscribeResultItem> Items { get; } = new List<MqttClientSubscribeResultItem>();
    }
}
