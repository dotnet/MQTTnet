using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public interface IMqttRetainedMessagesManager
    {
        Task Start(IMqttServerOptions options, IMqttNetChildLogger logger);

        Task LoadMessagesAsync();

        Task ClearMessagesAsync();

        Task HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage);

        Task<IList<MqttApplicationMessage>> GetMessagesAsync();

        Task<IList<MqttApplicationMessage>> GetSubscribedMessagesAsync(ICollection<TopicFilter> topicFilters);
    }
}
