// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Publish_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Return_Success_When_Subscribed()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient();
                var receiver = await testEnvironment.ConnectClient();

                await receiver.SubscribeAsync("A");

                // AtLeastOnce is required to get an ACK packet from the server.
                var publishResult = await sender.PublishStringAsync("A", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);
                
                Assert.AreEqual(MqttClientPublishReasonCode.Success, publishResult.ReasonCode);
                
                Assert.AreEqual(true, publishResult.IsSuccess);
            }
        }
        
        [TestMethod]
        public async Task Return_NoMatchingSubscribers_When_Not_Subscribed()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var sender = await testEnvironment.ConnectClient();
                var receiver = await testEnvironment.ConnectClient();
             
                await receiver.SubscribeAsync("A");

                // AtLeastOnce is required to get an ACK packet from the server.
                var publishResult = await sender.PublishStringAsync("B", "Payload", MqttQualityOfServiceLevel.AtLeastOnce);
                
                Assert.AreEqual(MqttClientPublishReasonCode.NoMatchingSubscribers, publishResult.ReasonCode);
                
                Assert.AreEqual(true, publishResult.IsSuccess);
            }
        }
    }
}