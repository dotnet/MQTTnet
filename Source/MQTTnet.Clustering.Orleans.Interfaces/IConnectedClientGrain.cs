using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public interface IConnectedClientGrain
    {

        ValueTask ConnectClient();

        ValueTask DisconnectClient();

        ValueTask EnqueueApplicationMessage(MqttApplicationMessage applicationMessage);

    }
}
