using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
#if NET5_0_OR_GREATER
using Microsoft.Extensions.Hosting;

namespace MQTTnet.Tests.Server
{

    [TestClass]
    public class Hosting_Tests
    {

        [TestMethod]
        public async Task Default_Host_Configuration()
        {

            var host = new HostBuilder()
                .UseMqttServer()
                .Build();
            await host.StartAsync();

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options
                    .WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [TestMethod]
        public async Task Custom_Host_Configuration()
        {

            var host = new HostBuilder()
                .UseMqttServer(mqtt =>
                {
                    mqtt
                        .WithDefaultEndpoint()
                        .WithKeepAlive();
                })
                .Build();
            await host.StartAsync();

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options
                    .WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [TestMethod]
        public async Task Advanced_Host_Configuration()
        {
            var syncLock = new object();
            var connectedClientCount = 0;
            var host = new HostBuilder()
                .UseMqttServer(mqtt =>
                {
                    mqtt
                        .WithDefaultEndpoint()
                        .WithKeepAlive();
                    
                    mqtt.ClientConnectedAsync += e =>
                    {
                        lock (syncLock)
                        {
                            connectedClientCount++;
                        }
                        return Task.CompletedTask;
                    };
                })
                .Build();

            await host.StartAsync();

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options
                    .WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
            Assert.AreEqual(1, connectedClientCount);
        }

    }
}

#endif