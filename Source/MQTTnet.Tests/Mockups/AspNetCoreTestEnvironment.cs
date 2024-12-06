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
using MQTTnet.LowLevelClient;
using MQTTnet.Protocol;
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

        protected override IMqttClient CreateClientCore()
        {
            return CreateClientFactory().CreateMqttClient();
        }

        protected override ILowLevelMqttClient CreateLowLevelClientCore()
        {
            return CreateClientFactory().CreateLowLevelMqttClient();
        }

        private IMqttClientFactory CreateClientFactory()
        {
            var services = new ServiceCollection();
            var clientBuilder = services.AddMqttClient();
            UseMqttLogger(clientBuilder, "[CLIENT]=>");
            return services.BuildServiceProvider().GetRequiredService<IMqttClientFactory>();
        }

        public override MqttServer CreateServer(MqttServerOptions options)
        {
            throw new NotSupportedException("Can not create MqttServer in AspNetCoreTestEnvironment.");
        }

        public override Task<MqttServer> StartServer(Action<MqttServerOptionsBuilder> configure)
        {
            var optionsBuilder = new MqttServerOptionsBuilder();
            configure?.Invoke(optionsBuilder);
            return StartServer(optionsBuilder);
        }

        public override Task<MqttServer> StartServer(MqttServerOptionsBuilder optionsBuilder)
        {
            optionsBuilder.WithDefaultEndpoint();
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);
            var serverOptions = optionsBuilder.Build();
            return StartServer(serverOptions);
        }

        private async Task<MqttServer> StartServer(MqttServerOptions serverOptions)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            if (serverOptions.DefaultEndpointOptions.Port == 0)
            {
                var serverPort = ServerPort > 0 ? ServerPort : GetServerPort();
                serverOptions.DefaultEndpointOptions.Port = serverPort;
            }

            var appBuilder = WebApplication.CreateBuilder();
            appBuilder.Services.AddSingleton(serverOptions);

            var serverBuilder = appBuilder.Services.AddMqttServer();
            UseMqttLogger(serverBuilder, "[SERVER]=>");

            appBuilder.WebHost.UseKestrel(k => k.ListenMqtt());
            appBuilder.Host.ConfigureHostOptions(h => h.ShutdownTimeout = TimeSpan.FromMilliseconds(500d));

            _app = appBuilder.Build();
            await _app.StartAsync();

            Server = _app.Services.GetRequiredService<MqttServer>();
            ServerPort = serverOptions.DefaultEndpointOptions.Port;

            Server.ValidatingConnectionAsync += e =>
            {
                if (TestContext != null)
                {
                    // Null is used when the client id is assigned from the server!
                    if (!string.IsNullOrEmpty(e.ClientId) && !e.ClientId.StartsWith(TestContext.TestName))
                    {
                        TrackException(new InvalidOperationException($"Invalid client ID used ({e.ClientId}). It must start with UnitTest name."));
                        e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                    }
                }

                return CompletedTask.Instance;
            };

            return Server;
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

        private void UseMqttLogger(IMqttBuilder builder, string categoryNamePrefix)
        {
            if (EnableLogger)
            {
                builder.UseAspNetCoreMqttNetLogger(l => l.CategoryNamePrefix = categoryNamePrefix);
            }
            else
            {
                builder.UseMqttNetNullLogger();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_app != null)
            {
                _app.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _app.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _app = null;
            }
        }
    }
}