using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Client.Publishing;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttHostedServer : IMqttServer, IHostedService
    {
        readonly MqttServer _mqttServer;

        public MqttHostedServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            _mqttServer = new MqttServer(options, adapters, logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = _mqttServer.StartAsync();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _mqttServer.StopAsync();
        }
        
        public Task<MqttClientPublishResult> PublishAsync(string senderClientId, MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            return _mqttServer.PublishAsync(senderClientId, applicationMessage, cancellationToken);
        }

        public void Dispose()
        {
            _mqttServer.Dispose();
        }

        public bool IsStarted => _mqttServer.IsStarted;
        
        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            return _mqttServer.GetClientStatusAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            return _mqttServer.GetSessionStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            return _mqttServer.GetRetainedApplicationMessagesAsync();
        }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            return _mqttServer.ClearRetainedApplicationMessagesAsync();
        }

        public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            return _mqttServer.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            return _mqttServer.UnsubscribeAsync(clientId, topicFilters);
        }

        public Task StartAsync()
        {
            return _mqttServer.StartAsync();
        }

        public Task StopAsync()
        {
            return _mqttServer.StopAsync();
        }
    }
}