// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.LowLevelClient;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestEnvironment : IDisposable
    {
        readonly List<string> _clientErrors = new();
        readonly List<IMqttClient> _clients = new();
        readonly List<Exception> _exceptions = new();
        readonly List<ILowLevelMqttClient> _lowLevelClients = new();
        readonly MqttProtocolVersion _protocolVersion;
        readonly List<string> _serverErrors = new();

        public TestEnvironment() : this(null)
        {
        }

        public TestEnvironment(TestContext testContext, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            _protocolVersion = protocolVersion;
            TestContext = testContext;

            TaskScheduler.UnobservedTaskException += TrackUnobservedTaskException;

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
                    if (!IgnoreClientLogErrors)
                    {
                        lock (_clientErrors)
                        {
                            _clientErrors.Add(e.LogMessage.ToString());
                        }
                    }
                }
            };
        }

        public MqttNetEventLogger ClientLogger { get; } = new("client");

        public static bool EnableLogger { get; set; } = true;

        public MqttClientFactory ClientFactory { get; } = new();

        public MqttServerFactory ServerFactory { get; } = new();

        public bool IgnoreClientLogErrors { get; set; }

        public bool IgnoreServerLogErrors { get; set; }

        public MqttServer Server { get; private set; }

        public MqttNetEventLogger ServerLogger { get; } = new("server");

        public int ServerPort { get; set; }

        public TestContext TestContext { get; }

        public Task<IMqttClient> ConnectClient()
        {
            return ConnectClient(ClientFactory.CreateClientOptionsBuilder().WithProtocolVersion(_protocolVersion));
        }

        public async Task<IMqttClient> ConnectClient(Action<MqttClientOptionsBuilder> configureOptions, TimeSpan timeout = default)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Start with initial default values.
            var optionsBuilder = ClientFactory.CreateClientOptionsBuilder().WithProtocolVersion(_protocolVersion).WithTcpServer("127.0.0.1", ServerPort);

            // Let the caller override settings. Do not touch the options after this.
            configureOptions.Invoke(optionsBuilder);

            var options = optionsBuilder.Build();

            var client = CreateClient();

            if (timeout == TimeSpan.Zero)
            {
                await client.ConnectAsync(options).ConfigureAwait(false);
            }
            else
            {
                using (var timeoutToken = new CancellationTokenSource(timeout))
                {
                    await client.ConnectAsync(options, timeoutToken.Token).ConfigureAwait(false);
                }
            }

            return client;
        }

        public async Task<IMqttClient> ConnectClient(MqttClientOptionsBuilder options, TimeSpan timeout = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options = options.WithTcpServer("127.0.0.1", ServerPort);

            var client = CreateClient();

            if (timeout == TimeSpan.Zero)
            {
                await client.ConnectAsync(options.Build()).ConfigureAwait(false);
            }
            else
            {
                using (var timeoutToken = new CancellationTokenSource(timeout))
                {
                    await client.ConnectAsync(options.Build(), timeoutToken.Token).ConfigureAwait(false);
                }
            }

            return client;
        }

        public async Task<IMqttClient> ConnectClient(MqttClientOptions options, TimeSpan timeout = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var client = CreateClient();

            if (timeout == TimeSpan.Zero)
            {
                await client.ConnectAsync(options).ConfigureAwait(false);
            }
            else
            {
                using (var timeoutToken = new CancellationTokenSource(timeout))
                {
                    await client.ConnectAsync(options, timeoutToken.Token).ConfigureAwait(false);
                }
            }

            return client;
        }

        public async Task<ILowLevelMqttClient> ConnectLowLevelClient(Action<MqttClientOptionsBuilder> optionsBuilder = null)
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

        public TestApplicationMessageReceivedHandler CreateApplicationMessageHandler(IMqttClient mqttClient)
        {
            return new TestApplicationMessageReceivedHandler(mqttClient);
        }

        public IMqttClient CreateClient()
        {
            var logger = EnableLogger ? (IMqttNetLogger)ClientLogger : MqttNetNullLogger.Instance;

            var client = ClientFactory.CreateMqttClient(logger);

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

        public MqttClientOptions CreateDefaultClientOptions()
        {
            return CreateDefaultClientOptionsBuilder().Build();
        }

        public MqttClientOptionsBuilder CreateDefaultClientOptionsBuilder()
        {
            return ClientFactory.CreateClientOptionsBuilder()
                .WithProtocolVersion(_protocolVersion)
                .WithTcpServer("127.0.0.1", ServerPort)
                .WithClientId(TestContext.TestName + "_" + Guid.NewGuid());
        }

        public ILowLevelMqttClient CreateLowLevelClient()
        {
            var client = ClientFactory.CreateLowLevelMqttClient(ClientLogger);

            lock (_lowLevelClients)
            {
                _lowLevelClients.Add(client);
            }

            return client;
        }

        public MqttServer CreateServer(MqttServerOptions options)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            var logger = EnableLogger ? (IMqttNetLogger)ServerLogger : new MqttNetNullLogger();

            Server = ServerFactory.CreateMqttServer(options, logger);

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

        public void Dispose()
        {
            try
            {
                lock (_clients)
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

                    _clients.Clear();
                }

                lock (_lowLevelClients)
                {
                    foreach (var lowLevelMqttClient in _lowLevelClients)
                    {
                        lowLevelMqttClient.Dispose();
                    }

                    _lowLevelClients.Clear();
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

                Server = null;

                ThrowIfLogErrors();

                GC.Collect();
                GC.WaitForFullGCComplete();
                GC.WaitForPendingFinalizers();

                if (_exceptions.Any())
                {
                    throw new Exception($"{_exceptions.Count} exceptions tracked.\r\n" + string.Join(Environment.NewLine, _exceptions));
                }
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= TrackUnobservedTaskException;
            }
        }

        public Task<MqttServer> StartServer()
        {
            return StartServer(ServerFactory.CreateServerOptionsBuilder());
        }

        public async Task<MqttServer> StartServer(MqttServerOptionsBuilder optionsBuilder)
        {
            optionsBuilder.WithDefaultEndpoint();
            optionsBuilder.WithDefaultEndpointPort(ServerPort);
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);

            var options = optionsBuilder.Build();
            var server = CreateServer(options);
            await server.StartAsync().ConfigureAwait(false);

            // The OS has chosen the port to we have to properly expose it to the tests.
            ServerPort = options.DefaultEndpointOptions.Port;
            return server;
        }

        public async Task<MqttServer> StartServer(Action<MqttServerOptionsBuilder> configure)
        {
            var optionsBuilder = ServerFactory.CreateServerOptionsBuilder();

            optionsBuilder.WithDefaultEndpoint();
            optionsBuilder.WithDefaultEndpointPort(ServerPort);
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);

            configure?.Invoke(optionsBuilder);

            var options = optionsBuilder.Build();
            var server = CreateServer(options);
            await server.StartAsync().ConfigureAwait(false);

            // The OS has chosen the port to we have to properly expose it to the tests.
            ServerPort = options.DefaultEndpointOptions.Port;
            return server;
        }

        public void ThrowIfLogErrors()
        {
            if (!IgnoreServerLogErrors)
            {
                lock (_serverErrors)
                {
                    if (_serverErrors.Count > 0)
                    {
                        var message = $"Server had {_serverErrors.Count} errors (${string.Join(Environment.NewLine, _serverErrors)}).";
                        Console.WriteLine(message);
                        throw new Exception(message);
                    }
                }
            }

            if (!IgnoreClientLogErrors)
            {
                lock (_clientErrors)
                {
                    if (_clientErrors.Count > 0)
                    {
                        var message = $"Client(s) had {_clientErrors.Count} errors (${string.Join(Environment.NewLine, _clientErrors)})";
                        Console.WriteLine(message);
                        throw new Exception(message);
                    }
                }
            }
        }

        public void TrackException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            lock (_exceptions)
            {
                _exceptions.Add(exception);
            }
        }

        void TrackUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            TrackException(e.Exception);
        }
    }
}