using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttServerBuilderTest
    {
        [TestMethod]
        public void AddMqttServerTest()
        {
            var services = new ServiceCollection();
            services.AddMqttServer();
            var s = services.BuildServiceProvider();

            var mqttServer1 = s.GetRequiredService<MqttServer>();
            var mqttServer2 = s.GetRequiredService<MqttServer>();
            Assert.IsInstanceOfType<AspNetCoreMqttServer>(mqttServer1);
            Assert.AreEqual(mqttServer1, mqttServer2);
        }

        [TestMethod]
        public void ConfigureMqttServerTest()
        {
            const int TcpKeepAliveTime1 = 19;
            const int TcpKeepAliveTime2 = 20;

            var services = new ServiceCollection();
            services.AddMqttServer().ConfigureMqttServer(
                b => b.WithTcpKeepAliveTime(TcpKeepAliveTime1),
                o =>
                {
                    Assert.AreEqual(TcpKeepAliveTime1, o.DefaultEndpointOptions.TcpKeepAliveTime);
                    o.DefaultEndpointOptions.TcpKeepAliveTime = TcpKeepAliveTime2;
                });

            var s = services.BuildServiceProvider();
            var options = s.GetRequiredService<MqttServerOptions>();
            Assert.AreEqual(TcpKeepAliveTime2, options.DefaultEndpointOptions.TcpKeepAliveTime);
        }

        [TestMethod]
        public void ConfigureMqttServerStopTest()
        {
            const string ReasonString1 = "ReasonString1";
            const string ReasonString2 = "ReasonString2";

            var services = new ServiceCollection();
            services.AddMqttServer().ConfigureMqttServerStop(
                b => b.WithDefaultClientDisconnectOptions(c => c.WithReasonString(ReasonString1)),
                o =>
                {
                    Assert.AreEqual(ReasonString1, o.DefaultClientDisconnectOptions.ReasonString);
                    o.DefaultClientDisconnectOptions.ReasonString = ReasonString2;
                });

            var s = services.BuildServiceProvider();
            var options = s.GetRequiredService<MqttServerStopOptions>();
            Assert.AreEqual(ReasonString2, options.DefaultClientDisconnectOptions.ReasonString);
        }

        [TestMethod]
        public void AddMqttServerAdapterTest()
        {
            var services = new ServiceCollection();
            services.AddMqttServer().AddMqttServerAdapter<MqttTcpServerAdapter>();
            services.AddMqttServer().AddMqttServerAdapter<MqttTcpServerAdapter>();

            var s = services.BuildServiceProvider();
            var adapters = s.GetRequiredService<IEnumerable<IMqttServerAdapter>>().ToArray();
            Assert.AreEqual(2, adapters.Length);
            Assert.IsInstanceOfType<AspNetCoreMqttServerAdapter>(adapters[0]);
            Assert.IsInstanceOfType<MqttTcpServerAdapter>(adapters[1]);
        }
    }
}
