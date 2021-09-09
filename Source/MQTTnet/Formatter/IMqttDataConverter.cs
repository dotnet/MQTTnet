using System.Collections.Generic;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter
{
    public interface IMqttDataConverter
    {
        MqttApplicationMessage CreateApplicationMessage(MqttPublishPacket publishPacket);

        MqttClientAuthenticateResult CreateClientConnectResult(MqttConnAckPacket connAckPacket);

        MqttClientPublishResult CreateClientPublishResult(MqttPubAckPacket pubAckPacket);

        MqttClientPublishResult CreateClientPublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket);
        
        MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket);
        
        MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket);

        MqttConnectPacket CreateConnectPacket(MqttApplicationMessage willApplicationMessage, IMqttClientOptions options);

        MqttConnAckPacket CreateConnAckPacket(MqttConnectionValidatorContext connectionValidatorContext);
        
        MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage);

        MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode);
        
        MqttPubRecPacket CreatePubRecPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode);

        MqttPubCompPacket CreatePubCompPacket(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode);
        
        MqttPubRelPacket CreatePubRelPacket(MqttPubRecPacket pubRecPacket, MqttApplicationMessageReceivedReasonCode reasonCode);
        
        MqttSubscribePacket CreateSubscribePacket(MqttClientSubscribeOptions options);

        MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult);

        MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options);

        MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket, List<MqttUnsubscribeReasonCode> reasonCodes);

        MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options);
    }
}
