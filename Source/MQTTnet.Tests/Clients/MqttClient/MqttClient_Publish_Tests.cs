// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Clients.MqttClient
{
    [TestClass]
    public sealed class MqttClient_Publish_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Acknowledge_After_Disconnect_Should_Fail()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient();
                await client.SubscribeAsync("TEST", MqttQualityOfServiceLevel.AtLeastOnce);

                var flag = false;
                
                client.ApplicationMessageReceivedAsync += async args =>
                {
                    try
                    {
                        args.AutoAcknowledge = false;
                        
                        await client.DisconnectAsync();
                        await args.AcknowledgeAsync();

                        flag = true;
                    }
                    catch (Exception exception)
                    {
                        
                    }
                };

                await server.InjectApplicationMessageAsync(
                    new MqttApplicationMessage
                    {
                        Topic = "TEST"
                    });

                SpinWait.SpinUntil(() => flag, TimeSpan.FromSeconds(10));
                
                Assert.IsTrue(flag);
            }
        }
    }
}