using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Internal
{
    sealed class AspNetCoreMqttHostedServer : IHostedService
    {
        private readonly AspNetCoreMqttServer _aspNetCoreMqttServer;

        public AspNetCoreMqttHostedServer(
            AspNetCoreMqttServer aspNetCoreMqttServer,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _aspNetCoreMqttServer = aspNetCoreMqttServer;
            hostApplicationLifetime.ApplicationStarted.Register(ApplicationStarted);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ApplicationStarted()
        {
            _ = _aspNetCoreMqttServer.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _aspNetCoreMqttServer.StopAsync();
        }
    }
}
