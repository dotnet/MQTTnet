using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Security_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Use_Username_Null_Password_Empty()
        {
            string username = null;
            var password = string.Empty;

            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithCredentials(username, password)
                    .Build();

                var ex = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(async () => await client.ConnectAsync(clientOptions));
                Assert.IsInstanceOfType(ex.InnerException, typeof(MqttProtocolViolationException));
                Assert.AreEqual("Error while authenticating. If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].", ex.Message, false);
            }
        }
        
        [TestMethod]
        public Task Handle_Wrong_UserName()
        {
            return TestCredentials("x", "Password1");
        }
        
        [TestMethod]
        public Task Handle_Wrong_Password()
        {
            return TestCredentials("UserName", "x");
        }
        
        [TestMethod]
        public Task Handle_Wrong_UserName_And_Password()
        {
            return TestCredentials("x", "x");
        }

        async Task TestCredentials(string userName, string password)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer(testEnvironment.Factory.CreateServerOptionsBuilder()
                    .WithConnectionValidator(c =>
                    {
                        if (c.Username != "UserName1")
                        {
                            c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        }
                        
                        if (c.Password != "Password1")
                        {
                            c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        }
                    }));

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithCredentials(userName, password)
                    .Build();

                var ex = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(async () => await client.ConnectAsync(clientOptions));
                Assert.AreEqual(ex.Result.ResultCode, MqttClientConnectResultCode.BadUserNameOrPassword);
            }
        }
    }
}