using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientQueuedStorage
    {
        Task SaveInflightMessagesAsync(IList<MqttApplicationMessage> messages);

        Task<IList<MqttApplicationMessage>> LoadInflightMessagesAsync();
    }
}
