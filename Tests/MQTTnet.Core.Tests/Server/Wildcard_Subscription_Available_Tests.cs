using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Wildcard_Subscription_Available_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Wildcard_Subscription_Available_Tests_Supported_V3()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsTrue(connectResult.WildcardSubscriptionAvailable);
            }
        }
        
        [TestMethod]
        public async Task Server_Reports_Wildcard_Subscription_Available_Tests_Supported_V5()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build());

                Assert.IsTrue(connectResult.WildcardSubscriptionAvailable);
            }
        }
    }
}