using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    public class NodeGrain : INodeGrain
    {
        private readonly MqttServer _mqttServer;

        public NodeGrain(MqttServer mqttServer)
        {
            _mqttServer = mqttServer;
        }

        public async ValueTask<IEnumerable<PublishMessageRoutineResult>> PublishMessagesAsync(IEnumerable<PublishMessageRoutine> routines)
        {
            var results = new List<PublishMessageRoutineResult>();

            var tasks = new List<Task>();
            foreach (var routine in routines)
            {
                tasks.Add(_mqttServer.InjectApplicationMessage(
                    new InjectedMqttApplicationMessage(
                        new MqttApplicationMessage
                        {
                            Topic = routine.Message.Topic,
                            Payload = routine.Message.Payload
                        })
                    {
                        SenderClientId = routine.Message.SenderClientId
                    }));
            }

            await Task.WhenAll(tasks);

            return results;
        }

        public ValueTask<IEnumerable<PublishMessageRoutineResult>> PublishMessagesAsync(PublishMessageBatch batch)
        {
            throw new NotImplementedException();
        }
    }
}
