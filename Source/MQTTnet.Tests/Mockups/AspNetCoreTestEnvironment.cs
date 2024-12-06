// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Mockups
{
    public sealed class AspNetCoreTestEnvironment : TestEnvironment
    {
        private WebApplication _app;

        public AspNetCoreTestEnvironment()
            : this(null)
        {
        }

        public AspNetCoreTestEnvironment(TestContext testContext, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
            : base(testContext, protocolVersion)
        {
        }

        public override IMqttClient CreateClient()
        {
            var services = new ServiceCollection();
            var clientBuilder = services.AddMqttClient();
            UseLogger(clientBuilder);

            var s = services.BuildServiceProvider();
            var client = s.GetRequiredService<IMqttClientFactory>().CreateMqttClient();

            client.ConnectingAsync += e =>
            {
                if (TestContext != null)
                {
                    var clientOptions = e.ClientOptions;
                    var existingClientId = clientOptions.ClientId;
                    if (existingClientId != null && !existingClientId.StartsWith(TestContext.TestName))
                    {
                        clientOptions.ClientId = TestContext.TestName + "_" + existingClientId;
                    }
                }

                return CompletedTask.Instance;
            };

            lock (_clients)
            {
                _clients.Add(client);
            }

            return client;
        }

        public override MqttServer CreateServer(MqttServerOptions options)
        {
            throw new NotSupportedException("Can not create MqttServer in AspNetCoreTestEnvironment.");
        }

        public override async Task<MqttServer> StartServer(Action<MqttServerOptionsBuilder> configure)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            var appBuilder = WebApplication.CreateBuilder();

            var serverBuilder = appBuilder.Services.AddMqttServer(optionsBuilder =>
            {
                optionsBuilder.WithDefaultEndpoint();
                optionsBuilder.WithDefaultEndpointPort(ServerPort);
                optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);
            }).ConfigureMqttServer(configure, o =>
            {
                if (o.DefaultEndpointOptions.Port == 0)
                {
                    var serverPort = GetServerPort();
                    o.DefaultEndpointOptions.Port = serverPort;
                    ServerPort = serverPort;
                }
            });

            UseLogger(serverBuilder);

            appBuilder.WebHost.UseKestrel(k => k.ListenMqtt());
            appBuilder.Host.ConfigureHostOptions(h => h.ShutdownTimeout = TimeSpan.FromMilliseconds(500d));

            _app = appBuilder.Build();
            await _app.StartAsync();

            Server = _app.Services.GetRequiredService<MqttServer>();
            return Server;
        }

        public override async Task<MqttServer> StartServer(MqttServerOptionsBuilder optionsBuilder)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            if (ServerPort == 0)
            {
                ServerPort = GetServerPort();
            }

            optionsBuilder.WithDefaultEndpoint();
            optionsBuilder.WithDefaultEndpointPort(ServerPort);
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);

            var appBuilder = WebApplication.CreateBuilder();
            appBuilder.Services.AddSingleton(optionsBuilder.Build());
            var serverBuilder = appBuilder.Services.AddMqttServer();

            UseLogger(serverBuilder);

            appBuilder.WebHost.UseKestrel(k => k.ListenMqtt());
            appBuilder.Host.ConfigureHostOptions(h => h.ShutdownTimeout = TimeSpan.FromMilliseconds(500d));

            _app = appBuilder.Build();
            await _app.StartAsync();

            Server = _app.Services.GetRequiredService<MqttServer>();
            return Server;
        }

        public override void Dispose()
        {
            if (_app != null)
            {
                _app.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _app.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _app = null;
            }
            base.Dispose();
        }

        private static int GetServerPort()
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            var portSet = listeners.Select(i => i.Port).ToHashSet();

            var port = 1883;
            while (!portSet.Add(port))
            {
                port += 1;
            }
            return port;
        }

        private void UseLogger(IMqttBuilder builder)
        {
            if (EnableLogger)
            {
                builder.UseAspNetCoreMqttNetLogger();
            }
            else
            {
                builder.UseMqttNetNullLogger();
            }
        }
    }
}