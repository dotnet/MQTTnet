using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace MQTTnet.AspNetCore
{
    public static class MqttSubProtocolSelector
    {
        public static string SelectSubProtocol(HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string subProtocol = null;
            if (request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
            {
                subProtocol = SelectSubProtocol(requestedSubProtocolValues);
            }

            return subProtocol;
        }
        
        public static string SelectSubProtocol(IList<string> requestedSubProtocolValues)
        {
            if (requestedSubProtocolValues == null) throw new ArgumentNullException(nameof(requestedSubProtocolValues));

            // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
            return requestedSubProtocolValues
                .OrderByDescending(p => p.Length)
                .FirstOrDefault(p => p.ToLower().StartsWith("mqtt"));
        }
    }
}
