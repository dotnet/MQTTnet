using MQTTnet.Client.Connecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter
{
    public interface IMqttDataConverter
    {
        MqttClientConnectResult CreateClientConnectResult(MqttConnAckPacket connAckPacket);

        MqttClientPublishResult CreateClientPublishResult(MqttPubAckPacket pubAckPacket);

        MqttClientPublishResult CreateClientPublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket);
        
        MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket);
        
        MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket);
        
        MqttPubCompPacket CreatePubCompPacket(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode);
        
        MqttPubRelPacket CreatePubRelPacket(MqttPubRecPacket pubRecPacket, MqttApplicationMessageReceivedReasonCode reasonCode);
        
        MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult);
        
        MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket, UnsubscribeResult unsubscribeResult);
    }
}
