#if NET5_0_OR_GREATER
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Hosting_Tests
    {
        [TestMethod]
        public async Task Advanced_Host_Configuration()
        {
            var syncLock = new object();
            var connectedClientCount = 0;
            var host = new HostBuilder().UseMqttServer(
                    mqtt =>
                    {
                        mqtt.WithDefaultEndpoint().WithKeepAlive();

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
                options.WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }

            Assert.AreEqual(1, connectedClientCount);
        }

        [TestMethod]
        public async Task Custom_Host_Configuration()
        {
            var host = new HostBuilder().UseMqttServer(
                    mqtt =>
                    {
                        mqtt.WithDefaultEndpoint().WithKeepAlive();
                    })
                .Build();
            await host.StartAsync();

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options.WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [TestMethod]
        public async Task Default_Host_Configuration()
        {
            var host = new HostBuilder().UseMqttServer().Build();
            await host.StartAsync();

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options.WithTcpServer("127.0.0.1");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [TestMethod]
        public async Task Default_WebSocket_Configuration_Connect()
        {
            var host = new HostBuilder().UseMqttServer(
                    mqtt =>
                    {
                        mqtt.WithDefaultWebSocketEndpoint().WithDefaultWebSocketEndpointPort(8080);
                    })
                .Build();
            await host.StartAsync();

            await Task.Delay(5000);

            // Perform client connect test
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = factory.CreateClientOptionsBuilder();
                options.WithWebSocketServer("127.0.0.1:8080/mqtt");

                await client.ConnectAsync(options.Build());
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [TestMethod]
        public async Task External_HttpListener_WebSocket_Configuration_Connect()
        {
            using (var tcs = new CancellationTokenSource())
            {
                var host = new HostBuilder().UseMqttServer().Build();
                await host.StartAsync();

                var httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://127.0.0.1:8080/");
                httpListener.Start();

                _ = Task.Factory.StartNew(
                    async () =>
                    {
                        while (!tcs.IsCancellationRequested)
                        {
                            try
                            {
                                var context = await httpListener.GetContextAsync();

                                if (context.Request.Url.AbsolutePath.Equals("/mqtt", StringComparison.OrdinalIgnoreCase) && context.Request.IsWebSocketRequest)
                                {
                                    var mqttServer = host.Services.GetService<MqttServer>();
                                    var webSocketContext = await context.AcceptWebSocketAsync("MQTT");
                                    mqttServer.HandleWebSocketConnection(webSocketContext, context);
                                }
                                else
                                {
                                    context.Response.StatusCode = 404;
                                    context.Response.Close();
                                }
                            }
                            catch
                            {
                            }
                        }
                    });

                await Task.Delay(5000);

                // Perform client connect test
                try
                {
                    var factory = new MqttFactory();
                    var client = factory.CreateMqttClient();
                    var options = factory.CreateClientOptionsBuilder();
                    options.WithWebSocketServer("127.0.0.1:8080/mqtt");

                    await client.ConnectAsync(options.Build());
                }
                finally
                {
                    await host.StopAsync();
                    httpListener.Stop();
                }
            }
        }
    }
}

#endif