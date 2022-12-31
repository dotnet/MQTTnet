using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Hosting
{
    public class MqttServerConfigurationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Action<MqttServer>> _configureActions;

        public MqttServerConfigurationHostedService(IServiceProvider serviceProvider, List<Action<MqttServer>> configureActions)
        {
            _serviceProvider = serviceProvider;
            _configureActions = configureActions;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var server = _serviceProvider.GetRequiredService<MqttServer>();
            _configureActions.ForEach(a => a(server));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            
            return Task.CompletedTask;
        }
    }
}
