using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServer : IDisposable
    {
        bool IsStarted { get; }
        
        Task<IList<IMqttClientStatus>> GetClientStatusAsync();
        Task<IList<IMqttSessionStatus>> GetSessionStatusAsync();

        Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync();
        Task ClearRetainedApplicationMessagesAsync();

        Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters);

        Task StartAsync();
        Task StopAsync();
    }
}