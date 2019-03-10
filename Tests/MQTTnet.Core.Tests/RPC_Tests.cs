using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Tests.Mockups;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Protocol;

namespace MQTTnet.Tests
{
    [TestClass]
    public class RPC_Tests
    {
        [TestMethod]
        public async Task Execute_Success()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();
                var responseSender = await testEnvironment.ConnectClientAsync();
                await responseSender.SubscribeAsync("MQTTnet.RPC/+/ping");

                responseSender.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    await responseSender.PublishAsync(e.ApplicationMessage.Topic + "/response", "pong");
                });

                var requestSender = await testEnvironment.ConnectClientAsync();

                var rpcClient = new MqttRpcClient(requestSender);
                var response = await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(5), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);

                Assert.AreEqual("pong", Encoding.UTF8.GetString(response));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        public async Task Execute_Timeout()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();
                
                var requestSender = await testEnvironment.ConnectClientAsync();

                var rpcClient = new MqttRpcClient(requestSender);
                await rpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
            }
        }
    }
}
