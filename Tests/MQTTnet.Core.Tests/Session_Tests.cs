using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Session_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Set_Session_Item()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var serverOptions = new MqttServerOptionsBuilder()
                    .WithConnectionValidator(delegate (MqttConnectionValidatorContext context)
                    {
                        // Don't validate anything. Just set some session items.
                        context.SessionItems["can_subscribe_x"] = true;
                        context.SessionItems["default_payload"] = "Hello World";
                    })
                    .WithSubscriptionInterceptor(delegate (MqttSubscriptionInterceptorContext context)
                    {
                        if (context.TopicFilter.Topic == "x")
                        {
                            context.AcceptSubscription = context.SessionItems["can_subscribe_x"] as bool? == true;
                        }
                    })
                    .WithApplicationMessageInterceptor(delegate (MqttApplicationMessageInterceptorContext context)
                    {
                        context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(context.SessionItems["default_payload"] as string);
                    });

                await testEnvironment.StartServerAsync(serverOptions);

                string receivedPayload = null;

                var client = await testEnvironment.ConnectClientAsync();
                client.UseApplicationMessageReceivedHandler(delegate (MqttApplicationMessageReceivedEventArgs args)
                {
                    receivedPayload = args.ApplicationMessage.ConvertPayloadToString();
                });

                var subscribeResult = await client.SubscribeAsync("x");

                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS0, subscribeResult.Items[0].ResultCode);

                var client2 = await testEnvironment.ConnectClientAsync();
                await client2.PublishAsync("x");

                await Task.Delay(1000);

                Assert.AreEqual("Hello World", receivedPayload);
            }
        }

        [TestMethod]
        public async Task Get_Session_Items_In_Status()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var serverOptions = new MqttServerOptionsBuilder()
                    .WithConnectionValidator(delegate (MqttConnectionValidatorContext context)
                    {
                        // Don't validate anything. Just set some session items.
                        context.SessionItems["can_subscribe_x"] = true;
                        context.SessionItems["default_payload"] = "Hello World";
                    });

                await testEnvironment.StartServerAsync(serverOptions);

                var client = await testEnvironment.ConnectClientAsync();

                var sessionStatus = await testEnvironment.Server.GetSessionStatusAsync();
                var session = sessionStatus.First();

                Assert.AreEqual(true, session.SessionItems["can_subscribe_x"]);
            }
        }


        [TestMethod]
        public async Task Manage_Session_MaxParallel()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;
                var serverOptions = new MqttServerOptionsBuilder();
                await testEnvironment.StartServerAsync(serverOptions);

                var options = new MqttClientOptionsBuilder().WithClientId("1");

                var clients = await Task.WhenAll(Enumerable.Range(0, 10)
                    .Select(i => TryConnect(testEnvironment, options)));

                var connectedClients = clients.Where(c => c?.IsConnected ?? false).ToList();

                Assert.AreEqual(1, connectedClients.Count);
            }
        }

        async Task<IMqttClient> TryConnect(TestEnvironment testEnvironment, MqttClientOptionsBuilder options)
        {
            try
            {
                return await testEnvironment.ConnectClientAsync(options);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
