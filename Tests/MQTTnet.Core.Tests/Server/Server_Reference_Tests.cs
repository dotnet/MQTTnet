using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Server_Reference_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_With_Reference_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;
                
                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    e.ReasonCode = MqttConnectReasonCode.ServerMoved;
                    e.ServerReference = "new_server";
                    
                    return Task.CompletedTask;
                };

                try
                {
                    var client = testEnvironment.CreateClient();
                    
                    await client.ConnectAsync(new MqttClientOptionsBuilder()
                        .WithProtocolVersion(MqttProtocolVersion.V500)
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .Build());
                    
                    Assert.Fail();
                }
                catch (MqttConnectingFailedException e)
                {
                    Assert.AreEqual(MqttClientConnectResultCode.ServerMoved, e.ResultCode);
                    Assert.AreEqual("new_server", e.Result.ServerReference);
                }
            }
        }
    }
}