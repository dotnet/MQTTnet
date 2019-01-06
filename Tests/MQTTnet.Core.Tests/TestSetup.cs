using System;
using System.Collections.Generic;
using System.Threading;
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

        private IMqttServer _server;

        private long _serverErrorsCount;
        private long _clientErrorsCount;

        public TestSetup()
        {
            _serverLogger.LogMessagePublished += (s, e) =>
            {
                if (e.TraceMessage.Level == MqttNetLogLevel.Error)
                {
                    Interlocked.Increment(ref _serverErrorsCount);
                }
            };

            _clientLogger.LogMessagePublished += (s, e) =>
            {
                if (e.TraceMessage.Level == MqttNetLogLevel.Error)
                {
                    Interlocked.Increment(ref _clientErrorsCount);
                }
            };
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

        public async Task<IMqttClient> ConnectClientAsync(MqttClientOptionsBuilder options)
        {
            var client = _mqttFactory.CreateMqttClient(_clientLogger);
            _clients.Add(client);

            await client.ConnectAsync(options.WithTcpServer("localhost", 1888).Build());

            return client;
        }

        public void ThrowIfLogErrors()
        {
            if (_serverErrorsCount > 0)
            {
                throw new Exception($"Server had {_serverErrorsCount} errors.");
            }

            if (_clientErrorsCount > 0)
            {
                throw new Exception($"Client(s) had {_clientErrorsCount} errors.");
            }
        }

        public void Dispose()
        {
            ThrowIfLogErrors();

            foreach (var mqttClient in _clients)
            {
                mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                mqttClient.Dispose();
            }

            _server.StopAsync().GetAwaiter().GetResult();
        }
    }
}
