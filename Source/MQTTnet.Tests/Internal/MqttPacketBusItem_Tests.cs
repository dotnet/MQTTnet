// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public sealed class MqttPacketBusItem_Tests
    {
        [TestMethod]
        public void Fire_Completed_Event()
        {
            var eventFired = false;

            var item = new MqttPacketBusItem(new MqttPublishPacket());
            item.Completed += (_, __) =>
            {
                eventFired = true;
            };

            item.Complete();

            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Wait_Packet_Bus_Item_After_Already_Canceled()
        {
            var item = new MqttPacketBusItem(new MqttPublishPacket());

            // Finish the item before the actual 
            item.Cancel();

            await item.WaitAsync();
        }
    }
}