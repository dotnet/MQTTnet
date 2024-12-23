using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttBuilderTest
    {
        [TestMethod]
        public void UseMqttNetNullLoggerTest()
        {
            var services = new ServiceCollection();
            services.AddMqttServer().UseMqttNetNullLogger();
            var s = services.BuildServiceProvider();
            var logger = s.GetRequiredService<IMqttNetLogger>();
            Assert.IsInstanceOfType<MqttNetNullLogger>(logger);
        }

        [TestMethod]
        public void UseAspNetCoreMqttNetLoggerTest()
        {
            var services = new ServiceCollection();
            services.AddMqttServer().UseAspNetCoreMqttNetLogger();
            var s = services.BuildServiceProvider();
            var logger = s.GetRequiredService<IMqttNetLogger>();
            Assert.IsInstanceOfType<AspNetCoreMqttNetLogger>(logger);
        }
    }
}
