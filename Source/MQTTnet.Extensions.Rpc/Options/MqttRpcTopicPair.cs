namespace MQTTnet.Extensions.Rpc.Options
{
    public sealed class MqttRpcTopicPair
    {
        public string RequestTopic { get; set; }

        public string ResponseTopic { get; set; }
    }
}
