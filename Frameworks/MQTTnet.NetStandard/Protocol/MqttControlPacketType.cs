namespace MQTTnet.Protocol
{
    public enum MqttControlPacketType
    {
        Connect = 1,
        ConnAck = 2,
        Publish = 3,
        PubAck = 4,
        PubRec = 5,
        PubRel = 6,
        PubComp = 7,
        Subscribe = 8,
        SubAck = 9,
        Unsubscibe = 10,
        UnsubAck = 11,
        PingReq = 12,
        PingResp = 13,
        Disconnect = 14
    }
}
