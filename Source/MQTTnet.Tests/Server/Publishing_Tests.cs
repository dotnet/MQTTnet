// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Publishing_Tests : BaseTestClass
    {
        [TestMethod]
        [ExpectedException(typeof(MqttClientDisconnectedException))]
        public async Task Disconnect_While_Publishing()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                // The client will be disconnect directly after subscribing!
                server.InterceptingPublishAsync += ev => server.DisconnectClientAsync(ev.ClientId, MqttDisconnectReasonCode.NormalDisconnection);

                var client = await testEnvironment.ConnectClient();
                await client.PublishStringAsync("test", qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce);
            }
        }
    }
}