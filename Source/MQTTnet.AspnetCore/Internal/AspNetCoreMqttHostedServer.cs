using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Internal
{
    sealed class AspNetCoreMqttHostedServer : IHostedService
    {
        private readonly AspNetCoreMqttServer _aspNetCoreMqttServer;

        public AspNetCoreMqttHostedServer(AspNetCoreMqttServer aspNetCoreMqttServer)
        {
            _aspNetCoreMqttServer = aspNetCoreMqttServer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _aspNetCoreMqttServer.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _aspNetCoreMqttServer.StopAsync();
        }
    }
}
