using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.Tests
{
    public class TestSetup : IDisposable
    {
        private readonly MqttFactory _mqttFactory = new MqttFactory();
        private readonly List<IMqttClient> _clients = new List<IMqttClient>();
        private readonly IMqttNetLogger _serverLogger = new MqttNetLogger("server");
        private readonly IMqttNetLogger _clientLogger = new MqttNetLogger("client");

        private readonly List<string> _serverErrors = new List<string>();
        private readonly List<string> _clientErrors = new List<string>();

        private readonly List<Exception> _exceptions = new List<Exception>();

        private IMqttServer _server;

        public bool IgnoreClientLogErrors { get; set; }
        public bool IgnoreServerLogErrors { get; set; }

        public TestSetup()
        {
            _serverLogger.LogMessagePublished += (s, e) =>
            {
                if (e.TraceMessage.Level == MqttNetLogLevel.Error)
                {
                    lock (_serverErrors)
                    {
                        _serverErrors.Add(e.TraceMessage.ToString());
                    }
                }
            };

            _clientLogger.LogMessagePublished += (s, e) =>
            {
                lock (_clientErrors)
                {
                    if (e.TraceMessage.Level == MqttNetLogLevel.Error)
                    {
                        _clientErrors.Add(e.TraceMessage.ToString());
                    }
                }
            };
        }

        public Task<IMqttServer> StartServerAsync()
        {
            return StartServerAsync(new MqttServerOptionsBuilder());
        }

        public async Task<IMqttServer> StartServerAsync(MqttServerOptionsBuilder options)
        {
            if (_server != null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            _server = _mqttFactory.CreateMqttServer(_serverLogger);
            await _server.StartAsync(options.WithDefaultEndpointPort(1888).Build());

            return _server;
        }

        public Task<IMqttClient> ConnectClientAsync()
        {
            return ConnectClientAsync(new MqttClientOptionsBuilder());
        }

        public async Task<IMqttClient> ConnectClientAsync(MqttClientOptionsBuilder options)
        {
            var client = _mqttFactory.CreateMqttClient(_clientLogger);
            await client.ConnectAsync(options.WithTcpServer("localhost", 1888).Build());

            _clients.Add(client);
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
                mqttClient?.DisconnectAsync().GetAwaiter().GetResult();
                mqttClient?.Dispose();
            }

            _server.StopAsync().GetAwaiter().GetResult();

            ThrowIfLogErrors();

            if (_exceptions.Any())
            {
                throw new Exception($"{_exceptions.Count} exceptions tracked.");
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
