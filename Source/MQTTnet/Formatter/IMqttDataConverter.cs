using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public interface IMqttDataConverter
    {
        MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage);

        MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket);

        MqttApplicationMessage CreateApplicationMessage(MqttPublishPacket publishPacket);

        MqttClientAuthenticateResult CreateClientConnectResult(MqttConnAckPacket connAckPacket);

        MqttConnectPacket CreateConnectPacket(MqttApplicationMessage willApplicationMessage, IMqttClientOptions options);

        MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket);

        MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket);

        MqttSubscribePacket CreateSubscribePacket(MqttClientSubscribeOptions options);

        MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options);

        MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options);

        MqttClientPublishResult CreatePublishResult(MqttPubAckPacket pubAckPacket);

        MqttClientPublishResult CreatePublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket);
    }
}
