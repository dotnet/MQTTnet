using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.AspnetCore
{
    public class MqttHostedServer : MqttServer, IHostedService
    {
        public MqttHostedServer(IOptions<MqttServerOptions> options, IEnumerable<IMqttServerAdapter> adapters, ILogger<MqttServer> logger, MqttClientSessionsManager clientSessionsManager) 
            : base(options, adapters, logger, clientSessionsManager)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync();
        }
    }
}
