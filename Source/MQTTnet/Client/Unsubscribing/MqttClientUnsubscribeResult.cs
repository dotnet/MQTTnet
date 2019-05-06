using System.Collections.Generic;

namespace MQTTnet.Client.Unsubscribing
{
    public class MqttClientUnsubscribeResult
    {
        public List<MqttClientUnsubscribeResultItem> Items { get; }  =new List<MqttClientUnsubscribeResultItem>();
    }
}
