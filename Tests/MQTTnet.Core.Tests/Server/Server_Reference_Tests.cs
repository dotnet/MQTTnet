using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Server_Reference_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Server_Reports_Topic_Alias_Supported()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(o =>
                {
                    o.WithConnectionValidator(v =>
                    {
                        v.ReasonCode = MqttConnectReasonCode.ServerMoved;
        
                    });
                });

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .Build());

                Assert.AreEqual(connectResult.ResultCode, MqttClientConnectResultCode.ServerMoved);
                Assert.AreEqual(connectResult.ServerReference, "new_server");
            }
        }
    }
}