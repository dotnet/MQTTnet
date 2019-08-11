using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Subscribing;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Session_Tests
    {
        [TestMethod]
        public async Task Set_Session_Item()
        {
            using (var testEnvironment = new TestEnvironment())
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
                client.UseApplicationMessageReceivedHandler(delegate(MqttApplicationMessageReceivedEventArgs args)
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
    }
}
