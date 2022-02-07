// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Mockups
{
    public sealed class MqttPacketAsserts
    {
        public void AssertIsConnectPacket(MqttBasePacket packet)
        {
            Assert.AreEqual(packet.GetType(), typeof(MqttConnectPacket));
        }
        
        public void AssertIsConnAckPacket(MqttBasePacket packet)
        {
            Assert.AreEqual(packet.GetType(), typeof(MqttConnAckPacket));
        }
    }
}