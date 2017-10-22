using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ServiceCollectionTest
    {
        [TestMethod]
        public void TestCanConstructAllServices()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddMqttServer()
                .AddMqttClient();

            var serviceProvider = services
                .BuildServiceProvider();

            foreach (var service in services)
            {
                if (service.ServiceType.IsGenericType)
                {
                    continue;
                }
                serviceProvider.GetRequiredService(service.ServiceType);
            }
        }
    }
}
