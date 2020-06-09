using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestEnvironment : Disposable
    {
        readonly MqttFactory _mqttFactory = new MqttFactory();
        readonly List<IMqttClient> _clients = new List<IMqttClient>();
        readonly IMqttNetLogger _serverLogger = new MqttNetLogger("server");
        readonly IMqttNetLogger _clientLogger = new MqttNetLogger("client");

        readonly List<string> _serverErrors = new List<string>();
        readonly List<string> _clientErrors = new List<string>();

        readonly List<Exception> _exceptions = new List<Exception>();

        public IMqttServer Server { get; private set; }

        public bool IgnoreClientLogErrors { get; set; }

        public bool IgnoreServerLogErrors { get; set; }

        public int ServerPort { get; set; } = 1888;

        public IMqttNetLogger ServerLogger => _serverLogger;

        public IMqttNetLogger ClientLogger => _clientLogger;

        public TestContext TestContext { get; }

        public TestEnvironment() : this(null)
        {
        }

        public TestEnvironment(TestContext testContext)
        {
            TestContext = testContext;

            _serverLogger.LogMessagePublished += (s, e) =>
            {
                if (e.LogMessage.Level == MqttNetLogLevel.Error)
                {
                    lock (_serverErrors)
                    {
                        _serverErrors.Add(e.LogMessage.ToString());
                    }
                }
            };

            _clientLogger.LogMessagePublished += (s, e) =>
            {
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
            var client = _mqttFactory.CreateMqttClient(_clientLogger);
            _clients.Add(client);

            return new TestClientWrapper(client, TestContext);
        }

        public Task<IMqttServer> StartServerAsync()
        {
            return StartServerAsync(new MqttServerOptionsBuilder());
        }

        public async Task<IMqttServer> StartServerAsync(MqttServerOptionsBuilder options)
        {
            if (Server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            Server = new TestServerWrapper(_mqttFactory.CreateMqttServer(_serverLogger), TestContext, this);
            await Server.StartAsync(options.WithDefaultEndpointPort(ServerPort).Build());

            return Server;
        }

        public Task<IMqttClient> ConnectClientAsync()
        {
            return ConnectClientAsync(new MqttClientOptionsBuilder());
        }

        public async Task<IMqttClient> ConnectClientAsync(MqttClientOptionsBuilder options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options = options.WithTcpServer("localhost", ServerPort);

            var client = CreateClient();
            await client.ConnectAsync(options.Build());

            return client;
        }

        public async Task<IMqttClient> ConnectClientAsync(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var client = CreateClient();
            await client.ConnectAsync(options);

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var mqttClient in _clients)
                {
                    mqttClient?.Dispose();
                }

                Server?.StopAsync().GetAwaiter().GetResult();

                ThrowIfLogErrors();

                if (_exceptions.Any())
                {
                    throw new Exception($"{_exceptions.Count} exceptions tracked.\r\n" + string.Join(Environment.NewLine, _exceptions));
                }
            }

            base.Dispose(disposing);
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