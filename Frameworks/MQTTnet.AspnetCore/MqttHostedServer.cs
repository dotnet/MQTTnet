using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttHostedServer : MqttServer, IHostedService
    {
        private readonly MqttServerOptions _options;

        public MqttHostedServer(
            MqttServerOptions options,
            IEnumerable<IMqttServerAdapter> adapters,
            IMqttNetLogger logger) : base(adapters, logger)
        {
            _options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartAsync(_options);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync();
        }
    }
}
