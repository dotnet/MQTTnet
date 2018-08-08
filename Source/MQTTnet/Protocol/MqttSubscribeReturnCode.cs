namespace MQTTnet.Protocol
{
    public enum MqttSubscribeReturnCode
    {
        SuccessMaximumQoS0 = 0x00,
        SuccessMaximumQoS1 = 0x01,
        SuccessMaximumQoS2 = 0x02,
        Failure = 0x80
    }
}
