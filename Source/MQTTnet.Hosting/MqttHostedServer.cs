using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MQTTnet.Server;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Hosting
{
    public sealed class MqttHostedServer : MqttServer, IHostedService
    {
        public MqttHostedServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
            : base(options, adapters, logger)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = StartAsync();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync();
        }
    }
}
