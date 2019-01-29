using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Server;

namespace MQTTnet.Tests.Mockups
{
    public class TestServerStorage : IMqttServerStorage
    {
        public IList<MqttApplicationMessage> Messages = new List<MqttApplicationMessage>();

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            Messages = messages;
            return Task.CompletedTask;
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            return Task.FromResult(Messages);
        }
    }
}
