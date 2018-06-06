using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClientStorage
    {
        Task SaveQueuedMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadQueuedMessagesAsync();
    }
}
