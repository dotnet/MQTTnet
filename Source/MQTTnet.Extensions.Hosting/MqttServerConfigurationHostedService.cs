using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting
{
    public class MqttServerConfigurationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<Action<MqttServer>> _startActions;
        private readonly List<Action<MqttServer>> _stopActions;

        public MqttServerConfigurationHostedService(IServiceProvider serviceProvider, List<Action<MqttServer>> startActions, List<Action<MqttServer>> stopActions)
        {
            _serviceProvider = serviceProvider;
            _startActions = startActions;
            _stopActions = stopActions;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var server = _serviceProvider.GetRequiredService<MqttServer>();
            _startActions.ForEach(a => a(server));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var server = _serviceProvider.GetRequiredService<MqttServer>();
            _stopActions.ForEach(a => a(server));

            return Task.CompletedTask;
        }
    }
}
