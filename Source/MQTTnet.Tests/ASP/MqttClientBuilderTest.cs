using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Implementations;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttClientBuilderTest
    {
        [TestMethod]
        public void AddMqttClientTest()
        {
            var services = new ServiceCollection();
            services.AddMqttClient();
            var s = services.BuildServiceProvider();

            var mqttClientFactory1 = s.GetRequiredService<IMqttClientFactory>();
            var mqttClientFactory2 = s.GetRequiredService<MqttClientFactory>();
            Assert.IsTrue(ReferenceEquals(mqttClientFactory2, mqttClientFactory2));

            Assert.IsInstanceOfType<AspNetCoreMqttClientFactory>(mqttClientFactory1);
            Assert.IsInstanceOfType<MqttClientFactory>(mqttClientFactory1);
        }

        [TestMethod]
        public void UseMQTTnetMqttClientAdapterFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddMqttClient().UseMQTTnetMqttClientAdapterFactory();
            var s = services.BuildServiceProvider();
            var adapterFactory = s.GetRequiredService<IMqttClientAdapterFactory>();

            Assert.IsInstanceOfType<MqttClientAdapterFactory>(adapterFactory);
        }


        [TestMethod]
        public void UseAspNetCoreMqttClientAdapterFactoryTest()
        {
            var services = new ServiceCollection();
            services.AddMqttClient().UseAspNetCoreMqttClientAdapterFactory();
            var s = services.BuildServiceProvider();
            var adapterFactory = s.GetRequiredService<IMqttClientAdapterFactory>();

            Assert.IsInstanceOfType<AspNetCoreMqttClientAdapterFactory>(adapterFactory);
        }
    }
}
