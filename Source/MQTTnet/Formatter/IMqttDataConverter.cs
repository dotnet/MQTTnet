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
using MqttClientSubscribeResult = MQTTnet.Client.Subscribing.MqttClientSubscribeResult;

namespace MQTTnet.Formatter
{
    public interface IMqttDataConverter
    {
        MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage);

        MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket);

        MqttBasePacket CreatePubRecPacket(MqttPublishPacket publishPacket);

        MqttApplicationMessage CreateApplicationMessage(MqttPublishPacket publishPacket);

        MqttClientAuthenticateResult CreateClientConnectResult(MqttConnAckPacket connAckPacket);

        MqttConnectPacket CreateConnectPacket(MqttApplicationMessage willApplicationMessage, IMqttClientOptions options);

        MqttConnAckPacket CreateConnAckPacket(MqttConnectionValidatorContext connectionValidatorContext);

        MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket);

        MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket);

        MqttSubscribePacket CreateSubscribePacket(MqttClientSubscribeOptions options);

        MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, Server.MqttClientSubscribeResult subscribeResult);

        MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options);

        MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket, List<MqttUnsubscribeReasonCode> reasonCodes);

        MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options);

        MqttClientPublishResult CreatePublishResult(MqttPubAckPacket pubAckPacket);

        MqttClientPublishResult CreatePublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket);
    }
}
