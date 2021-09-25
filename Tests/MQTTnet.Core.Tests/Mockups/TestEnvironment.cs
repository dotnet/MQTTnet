using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Formatter;
using MQTTnet.LowLevelClient;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestEnvironment : IDisposable
    {
        readonly MqttProtocolVersion _protocolVersion;
        readonly List<ILowLevelMqttClient> _lowLevelClients = new List<ILowLevelMqttClient>();
        readonly List<IMqttClient> _clients = new List<IMqttClient>();
        readonly List<string> _serverErrors = new List<string>();
        readonly List<string> _clientErrors = new List<string>();

        readonly List<Exception> _exceptions = new List<Exception>();

        public MqttFactory Factory { get; } = new MqttFactory();
        
        public IMqttServer Server { get; private set; }

        public bool IgnoreClientLogErrors { get; set; }

        public bool IgnoreServerLogErrors { get; set; }

        public int ServerPort { get; set; } = 1888;

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

        public IMqttClient CreateClient()
        {
            lock (_clients)
            {
                var client = Factory.CreateMqttClient(ClientLogger);
                _clients.Add(client);

                return new TestClientWrapper(client, TestContext);
            }
        }

        public Task<IMqttClient> ConnectClient()
        {
            return ConnectClient(Factory.CreateClientOptionsBuilder().WithProtocolVersion(_protocolVersion));
        }

        public async Task<IMqttClient> ConnectClient(Action<MqttClientOptionsBuilder> optionsBuilder)
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
        
        public async Task<IMqttClient> ConnectClient(MqttClientOptionsBuilder options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options = options.WithTcpServer("127.0.0.1", ServerPort);

            var client = CreateClient();
            await client.ConnectAsync(options.Build()).ConfigureAwait(false);

            return client;
        }

        public async Task<IMqttClient> ConnectClient(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var client = CreateClient();
            await client.ConnectAsync(options).ConfigureAwait(false);

            return client;
        }
        
        public ILowLevelMqttClient CreateLowLevelClient()
        {
            lock (_clients)
            {
                var client = Factory.CreateLowLevelMqttClient(ClientLogger);
                _lowLevelClients.Add(client);

                return client;
            }
        }
        
        public Task<ILowLevelMqttClient> ConnectLowLevelClient()
        {
            return ConnectLowLevelClient(o => { });
        }

        public async Task<ILowLevelMqttClient> ConnectLowLevelClient(Action<MqttClientOptionsBuilder> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            var options = new MqttClientOptionsBuilder();
            options = options.WithTcpServer("127.0.0.1", ServerPort);
            optionsBuilder.Invoke(options);

            var client = CreateLowLevelClient();
            await client.ConnectAsync(options.Build(), CancellationToken.None).ConfigureAwait(false);

            return client;
        }
        
        public async Task<IMqttRpcClient> ConnectRpcClient(IMqttRpcClientOptions options)
        {
            return new MqttRpcClient(await ConnectClient(), options);
        }

        public IMqttServer CreateServer()
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            Server = new TestServerWrapper(Factory.CreateMqttServer(ServerLogger), TestContext, this);
            return Server;
        }
        
        public Task<IMqttServer> StartServer()
        {
            return StartServer(Factory.CreateServerOptionsBuilder());
        }

        public async Task<IMqttServer> StartServer(MqttServerOptionsBuilder options)
        {
            var server = CreateServer();
            
            options.WithDefaultEndpointPort(ServerPort);
            options.WithMaxPendingMessagesPerClient(int.MaxValue);
            
            await server.StartAsync(options.Build());

            return server;
        }
        
        public async Task<IMqttServer> StartServer(Action<MqttServerOptionsBuilder> options)
        {
            var server = CreateServer();

            var optionsBuilder = Factory.CreateServerOptionsBuilder();
            optionsBuilder.WithDefaultEndpointPort(ServerPort);
            optionsBuilder.WithMaxPendingMessagesPerClient(int.MaxValue);
            
            options?.Invoke(optionsBuilder);
            
            await server.StartAsync(optionsBuilder.Build());

            return server;
        }
        
        public TestApplicationMessageReceivedHandler CreateApplicationMessageHandler(IMqttClient mqttClient)
        {
            if (mqttClient == null) throw new ArgumentNullException(nameof(mqttClient));

            var handler = new TestApplicationMessageReceivedHandler();
            if (mqttClient.ApplicationMessageReceivedHandler != null)
            {
                throw new InvalidOperationException("ApplicationMessageReceivedHandler is already set.");
            }

            mqttClient.ApplicationMessageReceivedHandler = handler;
            
            return handler;
        }

        public void ThrowIfLogErrors()
        {
            lock (_serverErrors)
            {
                if (!IgnoreServerLogErrors && _serverErrors.Count > 0)
                {
                    throw new Exception($"Server had {_serverErrors.Count} errors (${string.Join(Environment.NewLine, _serverErrors)}).");
                }
            }

            lock (_clientErrors)
            {
                if (!IgnoreClientLogErrors && _clientErrors.Count > 0)
                {
                    throw new Exception($"Client(s) had {_clientErrors.Count} errors (${string.Join(Environment.NewLine, _clientErrors)}).");
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
                    mqttClient.DisconnectAsync().GetAwaiter().GetResult();
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