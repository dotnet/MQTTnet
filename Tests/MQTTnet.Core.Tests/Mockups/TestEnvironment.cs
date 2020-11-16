using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.LowLevelClient;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestEnvironment : IDisposable
    {
        readonly MqttFactory _mqttFactory = new MqttFactory();
        readonly List<IMqttClient> _clients = new List<IMqttClient>();
        readonly List<string> _serverErrors = new List<string>();
        readonly List<string> _clientErrors = new List<string>();

        readonly List<Exception> _exceptions = new List<Exception>();

        public IMqttServer Server { get; private set; }

        public bool IgnoreClientLogErrors { get; set; }

        public bool IgnoreServerLogErrors { get; set; }

        public int ServerPort { get; set; } = 1888;

        public MqttNetLogger ServerLogger { get; } = new MqttNetLogger("server");

        public MqttNetLogger ClientLogger { get; } = new MqttNetLogger("client");

        public TestContext TestContext { get; }

        public TestEnvironment() : this(null)
        {
        }

        public TestEnvironment(TestContext testContext)
        {
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
                var client = _mqttFactory.CreateMqttClient(ClientLogger);
                _clients.Add(client);

                return new TestClientWrapper(client, TestContext);
            }
        }

        public Task<IMqttServer> StartServerAsync()
        {
            return StartServerAsync(new MqttServerOptionsBuilder());
        }

        public async Task<IMqttServer> StartServerAsync(MqttServerOptionsBuilder options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            Server = new TestServerWrapper(_mqttFactory.CreateMqttServer(ServerLogger), TestContext, this);

            options.WithDefaultEndpointPort(ServerPort);

            await Server.StartAsync(options.Build()).ConfigureAwait(false);

            return Server;
        }

        public Task<IMqttClient> ConnectClientAsync()
        {
            return ConnectClientAsync(new MqttClientOptionsBuilder());
        }

        public Task<ILowLevelMqttClient> ConnectLowLevelClientAsync()
        {
            return ConnectLowLevelClientAsync(o => { });
        }

        public async Task<ILowLevelMqttClient> ConnectLowLevelClientAsync(Action<MqttClientOptionsBuilder> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            var options = new MqttClientOptionsBuilder();
            options = options.WithTcpServer("127.0.0.1", ServerPort);
            optionsBuilder.Invoke(options);

            var client = new MqttFactory().CreateLowLevelMqttClient();
            await client.ConnectAsync(options.Build(), CancellationToken.None).ConfigureAwait(false);

            return client;
        }

        public async Task<IMqttClient> ConnectClientAsync(Action<MqttClientOptionsBuilder> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            var options = new MqttClientOptionsBuilder();
            options = options.WithTcpServer("localhost", ServerPort);
            optionsBuilder.Invoke(options);

            var client = CreateClient();
            await client.ConnectAsync(options.Build()).ConfigureAwait(false);

            return client;
        }

        public async Task<IMqttClient> ConnectClientAsync(MqttClientOptionsBuilder options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options = options.WithTcpServer("localhost", ServerPort);

            var client = CreateClient();
            await client.ConnectAsync(options.Build()).ConfigureAwait(false);

            return client;
        }

        public async Task<IMqttClient> ConnectClientAsync(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var client = CreateClient();
            await client.ConnectAsync(options).ConfigureAwait(false);

            return client;
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

        public void TrackException(Exception exception)
        {
            lock (_exceptions)
            {
                _exceptions.Add(exception);
            }
        }
    }
}