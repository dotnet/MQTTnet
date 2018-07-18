using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Exceptions;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttClientTests
    {

        [TestMethod]
        public async Task ClientDisconnectException()
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();

            Exception ex = null;
            client.Disconnected += (s, e) =>
            {
                ex = e.Exception;
            };

            try
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("wrong-server").Build());
            }
            catch
            {
            }

            Assert.IsNotNull(ex);
            Assert.IsInstanceOfType(ex, typeof(MqttCommunicationException));
            Assert.IsInstanceOfType(ex.InnerException, typeof(SocketException));
        }
    }
}
