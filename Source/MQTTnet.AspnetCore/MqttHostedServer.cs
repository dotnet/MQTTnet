using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttHostedServer : MqttServer, IHostedService
    {
        private readonly IMqttServerOptions _options;

        public MqttHostedServer(IMqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger) 
            : base(adapters, logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = StartAsync(_options);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync();
        }
    }
}
