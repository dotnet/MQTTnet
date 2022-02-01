using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MQTTnet.Tests.Diagnostics
{
    [TestClass]
    public sealed class PacketInspection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Inspect_Client_Packets()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
            
                using (var mqttClient = testEnvironment.CreateClient())
                {
                    var mqttClientOptions = testEnvironment.Factory.CreateClientOptionsBuilder()
                        .WithClientId("CLIENT_ID") // Must be fixed.
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .Build();

                    var packets = new List<string>();
                    
                    mqttClient.InspectPackage += eventArgs =>
                    {
                        packets.Add(eventArgs.Direction + ":" + Convert.ToBase64String(eventArgs.Buffer));
                        return Task.CompletedTask;
                    };
                
                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                
                    Assert.AreEqual(2, packets.Count);
                    Assert.AreEqual("Outbound:ECwABE1RVFQEAgAPACBJbnNwZWN0X0NsaWVudF9QYWNrZXRzX0NMSUVOVF9JRA==", packets[0]); // CONNECT
                    Assert.AreEqual("Inbound:IAIAAA==", packets[1]); // CONNACK
                }
            }
        }
    }
}