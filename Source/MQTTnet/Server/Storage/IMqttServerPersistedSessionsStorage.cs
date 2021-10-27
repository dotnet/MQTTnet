using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerPersistedSessionsStorage
    {
        Task<IList<IMqttServerPersistedSession>> LoadPersistedSessionsAsync();
    }
}
