using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Implementations;
using MQTTnet.Server;

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

            var exceptionIsCorrect = false;
            client.Disconnected += (s, e) =>
            {
                exceptionIsCorrect = e.Exception is MqttCommunicationException c && c.InnerException is SocketException;
            };

            try
            {
                await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("wrong-server").Build());
            }
            catch
            {
            }
            
            Assert.IsTrue(exceptionIsCorrect);
        }
    }
}
