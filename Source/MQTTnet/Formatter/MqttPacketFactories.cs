// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketFactories
    {
        public MqttConnAckPacketFactory ConnAck { get; } = new MqttConnAckPacketFactory();
        
        public MqttConnectPacketFactory Connect { get; } = new MqttConnectPacketFactory();

        public MqttDisconnectPacketFactory Disconnect { get; } = new MqttDisconnectPacketFactory();

        public MqttPubAckPacketFactory PubAck { get; } = new MqttPubAckPacketFactory();

        public MqttPubCompPacketFactory PubComp { get; } = new MqttPubCompPacketFactory();

        public MqttPublishPacketFactory Publish { get; } = new MqttPublishPacketFactory();

        public MqttPubRecPacketFactory PubRec { get; } = new MqttPubRecPacketFactory();

        public MqttPubRelPacketFactory PubRel { get; } = new MqttPubRelPacketFactory();

        public MqttSubAckPacketFactory SubAck { get; } = new MqttSubAckPacketFactory();

        public MqttSubscribePacketFactory Subscribe { get; } = new MqttSubscribePacketFactory();

        public MqttUnsubAckPacketFactory UnsubAck { get; } = new MqttUnsubAckPacketFactory();

        public MqttUnsubscribePacketFactory Unsubscribe { get; } = new MqttUnsubscribePacketFactory();
    }
}