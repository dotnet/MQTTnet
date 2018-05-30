using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.ManagedClient
{
    public interface IManagedMqttClientStorage
    {
        Task SaveQueuedMessagesAsync(IList<MqttApplicationMessageId> messages);

        Task<IList<MqttApplicationMessageId>> LoadQueuedMessagesAsync();
    }
}
