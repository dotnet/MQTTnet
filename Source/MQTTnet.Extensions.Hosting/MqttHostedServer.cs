using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using MQTTnet.Server;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.Hosting
{
    public sealed class MqttHostedServer : MqttServer, IHostedService
    {
        public MqttHostedServer(IServiceProvider serviceProvider, MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
            : base(options, adapters, logger)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync(new MqttServerStopOptions());
        }
    }
}
