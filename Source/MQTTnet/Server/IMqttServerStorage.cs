using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerStorage
    {
        Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync();
    }

    public interface IMqttExtendedServerStorage : IMqttServerStorage
    {
        Task<bool> HandleMessageAsync(string clientId, MqttApplicationMessage applicationMessage);

        Task<IList<MqttApplicationMessage>> GetMessagesAsync();

        Task<List<MqttApplicationMessage>> GetSubscribedMessagesAsync(ICollection<TopicFilter> topicFilters);

        Task ClearMessagesAsync();
    }
}
