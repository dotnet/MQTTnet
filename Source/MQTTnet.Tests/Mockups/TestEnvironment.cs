// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;
using MqttClient = MQTTnet.Client.MqttClient;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestEnvironment : IDisposable
    {
        readonly MqttProtocolVersion _protocolVersion;
        readonly List<LowLevelMqttClient> _lowLevelClients = new List<LowLevelMqttClient>();
        readonly List<MqttClient> _clients = new List<MqttClient>();
        readonly List<string> _serverErrors = new List<string>();
        readonly List<string> _clientErrors = new List<string>();

        readonly List<Exception> _exceptions = new List<Exception>();

        public MqttFactory Factory { get; } = new MqttFactory();
        
        public MqttServer Server { get; private set; }

        public bool IgnoreClientLogErrors { get; set; }

        public bool IgnoreServerLogErrors { get; set; }

        public int ServerPort { get; set; } = 1888;

        public static bool EnableLogger { get; set; } = true;
        
        public MqttNetEventLogger ServerLogger { get; } = new MqttNetEventLogger("server");

        public MqttNetEventLogger ClientLogger { get; } = new MqttNetEventLogger("client");

        public TestContext TestContext { get; }

        public TestEnvironment() : this(null)
        {
        }

        public TestEnvironment(TestContext testContext, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            _protocolVersion = protocolVersion;
            TestContext = testContext;

            ServerLogger.LogMessagePublished += (s, e) =>
            {
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(e.LogMessage.ToString());
                }

                if (e.LogMessage.Level == MqttNetLogLevel.Error)
                {
                    lock (_serverErrors)
                    {
                        _serverErrors.Add(e.LogMessage.ToString());
                    }
                }
            };

            ClientLogger.LogMessagePublished += (s, e) =>
            {
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(e.LogMessage.ToString());
                }

                if (e.LogMessage.Level == MqttNetLogLevel.Error)
                {
                    lock (_clientErrors)
                    {
                        _clientErrors.Add(e.LogMessage.ToString());
                    }
                }
            };
        }

        public MqttClient CreateClient()
        {
            lock (_clients)
            {
                var logger = EnableLogger ? (IMqttNetLogger)ClientLogger : new MqttNetNullLogger();
                
                var client = Factory.CreateMqttClient(logger);
                _clients.Add(client);

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

                    return PlatformAbstractionLayer.CompletedTask;
                };

                return client;
            }
        }

        public Task<MqttClient> ConnectClient()
        {
            return ConnectClient(Factory.CreateClientOptionsBuilder().WithProtocolVersion(_protocolVersion));
        }

        public async Task<MqttClient> ConnectClient(Action<MqttClientOptionsBuilder> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            var options = Factory.CreateClientOptionsBuilder()
                .WithProtocolVersion(_protocolVersion)
                .WithTcpServer("127.0.0.1", ServerPort);
            
            optionsBuilder.Invoke(options);

            var client = CreateClient();
            await client.ConnectAsync(options.Build()).ConfigureAwait(false);

            return client;
        }
        
        public async Task<MqttClient> ConnectClient(MqttClientOptionsBuilder options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options = options.WithTcpServer("127.0.0.1", ServerPort);

            var client = CreateClient();
            await client.ConnectAsync(options.Build()).ConfigureAwait(false);

            return client;
        }

        public async Task<MqttClient> ConnectClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var client = CreateClient();
            await client.ConnectAsync(options).ConfigureAwait(false);

            return client;
        }
        
        public LowLevelMqttClient CreateLowLevelClient()
        {
            lock (_clients)
            {
                var client = Factory.CreateLowLevelMqttClient(ClientLogger);
                _lowLevelClients.Add(client);

                return client;
            }
        }
        
        public async Task<LowLevelMqttClient> ConnectLowLevelClient(Action<MqttClientOptionsBuilder> optionsBuilder = null)
        {
            var options = new MqttClientOptionsBuilder();
            options = options.WithTcpServer("127.0.0.1", ServerPort);
            optionsBuilder?.Invoke(options);

            var client = CreateLowLevelClient();
            await client.ConnectAsync(options.Build(), CancellationToken.None).ConfigureAwait(false);

            return client;
        }
        
        public async Task<MqttRpcClient> ConnectRpcClient(MqttRpcClientOptions options)
        {
            return new MqttRpcClient(await ConnectClient(), options);
        }

        public MqttServer CreateServer(MqttServerOptions options)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            var logger = EnableLogger ? (IMqttNetLogger)ServerLogger : new MqttNetNullLogger();
            
            Server = Factory.CreateMqttServer(options, logger);

            Server.ValidatingConnectionAsync += e =>
            {
                if (TestContext != null)
                {
                    // Null is used when the client id is assigned from the server!
                    if (!string.IsNullOrEmpty(e.ClientId) && !e.ClientId.StartsWith(TestContext.TestName))
                    {
                        TrackException(new InvalidOperationException($"Invalid client ID used ({e.ClientId}). It must start with UnitTest name."));
                        e.ReasonCode = Protocol.MqttConnectReasonCode.ClientIdentifierNotValid;
                    }
                }

                return Task.CompletedTask;
            };
            
            return Server;
        }
        
        public Task<MqttServer> StartServer()
        {
            return StartServer(Factory.CreateServerOptionsBuilder());
        }

        public async Task<MqttServer> StartServer(MqttServerOptionsBuilder options)
        {
            var server = CreateServer(options.Build());

            options.WithDefaultEndpoint();
            options.WithDefaultEndpointPort(ServerPort);
            options.WithMaxPendingMessagesPerClient(int.MaxValue);
            
            await server.StartAsync();

            return server;
        }
        
        public async Task<MqttServer> StartServer(Action<MqttServerOptionsBuilder> configure)
        {
            var optionsBuilder = Factory.CreateServerOptionsBuilder();
            
            optionsBuilder.WithDefaultEndpoint();
            optionsBuilder.WithDefaultEndpointPort(ServerPort);
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);

            configure?.Invoke(optionsBuilder);
            
            var server = CreateServer(optionsBuilder.Build());
            await server.StartAsync();
            return server;
        }
        
        public TestApplicationMessageReceivedHandler CreateApplicationMessageHandler(MqttClient mqttClient)
        {
            return new TestApplicationMessageReceivedHandler(mqttClient);
        }

        public void ThrowIfLogErrors()
        {
            lock (_serverErrors)
            {
                if (!IgnoreServerLogErrors && _serverErrors.Count > 0)
                {
                    var message = $"Server had {_serverErrors.Count} errors (${string.Join(Environment.NewLine, _serverErrors)}).";
                    Console.WriteLine(message);
                    throw new Exception(message);
                }
            }

            lock (_clientErrors)
            {
                if (!IgnoreClientLogErrors && _clientErrors.Count > 0)
                {
                    var message = $"Client(s) had {_clientErrors.Count} errors (${string.Join(Environment.NewLine, _clientErrors)})";
                    Console.WriteLine(message);
                    throw new Exception(message);
                }
            }
        }

        public void TrackException(Exception exception)
        {
            lock (_exceptions)
            {
                _exceptions.Add(exception);
            }
        }
        
        public void Dispose()
        {
            foreach (var mqttClient in _clients)
            {
                try
                {
                    //mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // This can happen when the test already disconnected the client.
                }
                finally
                {
                    mqttClient?.Dispose();
                }
            }

            foreach (var lowLevelMqttClient in _lowLevelClients)
            {
                lowLevelMqttClient.Dispose();
            }

            try
            {
                Server?.StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // This can happen when the test already stopped the server.
            }
            finally
            {
                Server?.Dispose();
            }

            ThrowIfLogErrors();

            if (_exceptions.Any())
            {
                throw new Exception($"{_exceptions.Count} exceptions tracked.\r\n" + string.Join(Environment.NewLine, _exceptions));
            }
        }
    }
}