using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.AspNetCore
{
    public static class MqttSubProtocolSelector
    {
        public static string SelectSubProtocol(IList<string> requestedSubProtocolValues)
        {
            // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
            return requestedSubProtocolValues
                .OrderByDescending(p => p.Length)
                .FirstOrDefault(p => p.ToLower().StartsWith("mqtt"));
        }
    }
}
