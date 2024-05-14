// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public static class MqttPacketFactories
    {
        public static MqttConnectPacketFactory Connect { get; } = new();

        public static MqttDisconnectPacketFactory Disconnect { get; } = new();

        public static MqttPubAckPacketFactory PubAck { get; } = new();

        public static MqttPubCompPacketFactory PubComp { get; } = new();

        public static MqttPublishPacketFactory Publish { get; } = new();

        public static MqttPubRecPacketFactory PubRec { get; } = new();

        public static MqttPubRelPacketFactory PubRel { get; } = new();

        public static MqttSubscribePacketFactory Subscribe { get; } = new();

        public static MqttUnsubscribePacketFactory Unsubscribe { get; } = new();
    }
}