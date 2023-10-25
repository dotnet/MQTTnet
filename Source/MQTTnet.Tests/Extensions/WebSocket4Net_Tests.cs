// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.WebSocket4Net;
using MQTTnet.Tests.Helpers;

namespace MQTTnet.Tests.Extensions
{
    [TestClass]
    public sealed class WebSocket4Net_Tests
    {
        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_Failed_With_Invalid_Server()
        {
            var factory = new MqttFactory().UseWebSocket4Net();

            using (var client = factory.CreateMqttClient())
            {
                var options = new MqttClientOptionsBuilder().WithWebSocketServer(o => o.WithUri("ws://a.b/mqtt")).WithTimeout(TimeSpan.FromSeconds(2)).Build();
                await client.ConnectAsync(options).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void Use_Correct_Adapter()
        {
            // Should not throw exception
            var factory = new MqttFactory().UseWebSocket4Net();
            using (var client = factory.CreateMqttClient())
            {
                var adapterFactory = client.GetFieldValue("_adapterFactory");

                Assert.IsInstanceOfType(adapterFactory, typeof(WebSocket4NetMqttClientAdapterFactory));
            }
        }

        [TestMethod]
        public void Use_WebSocket4Net()
        {
            // Should not throw exception
            new MqttFactory().UseWebSocket4Net();
        }
    }
}