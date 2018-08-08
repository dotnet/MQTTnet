namespace MQTTnet.Protocol
{
    public enum MqttQualityOfServiceLevel
    {
        AtMostOnce = 0x00,
        AtLeastOnce = 0x01,
        ExactlyOnce = 0x02
    }
}
