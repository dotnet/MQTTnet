using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.ManagedClient
{
    public interface IManagedMqttClientStorage
    {
        Task SaveQueuedMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadQueuedMessagesAsync();
    }
}
