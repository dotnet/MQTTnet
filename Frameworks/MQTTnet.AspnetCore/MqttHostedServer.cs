using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttHostedServer : MqttServer, IHostedService
    {
        public MqttHostedServer(
            IOptions<MqttServerOptions> options,
            IEnumerable<IMqttServerAdapter> adapters,
            ILogger<MqttServer> logger, 
            MqttClientSessionsManager clientSessionsManager,
            IMqttClientRetainedMessageManager clientRetainedMessageManager
            ) 
            : base(options, adapters, logger, clientSessionsManager, clientRetainedMessageManager)
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
