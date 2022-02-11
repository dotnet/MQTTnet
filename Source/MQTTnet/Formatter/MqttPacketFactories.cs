// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketFactories
    {
        public MqttConnectPacketFactory Connect { get; } = new MqttConnectPacketFactory();
        
        public MqttConnAckPacketFactory ConnAck { get; } = new MqttConnAckPacketFactory();

        public MqttDisconnectPacketFactory Disconnect { get; } = new MqttDisconnectPacketFactory();

        public MqttSubscribePacketFactory Subscribe { get; } = new MqttSubscribePacketFactory();

        public MqttSubAckPacketFactory SubAck { get; } = new MqttSubAckPacketFactory();

        public MqttUnsubscribePacketFactory Unsubscribe { get; } = new MqttUnsubscribePacketFactory();

        public MqttUnsubAckPacketFactory UnsubAck { get; } = new MqttUnsubAckPacketFactory();

        public MqttPublishPacketFactory Publish { get; } = new MqttPublishPacketFactory();

        public MqttPubAckPacketFactory PubAck { get; } = new MqttPubAckPacketFactory();

        public MqttPubRelPacketFactory PubRel { get; } = new MqttPubRelPacketFactory();
        
        public MqttPubRecPacketFactory PubRec { get; } = new MqttPubRecPacketFactory();
        
        public MqttPubCompPacketFactory PubComp { get; } = new MqttPubCompPacketFactory();
    }
}