using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public class MqttClientOptionsBuilderWebSocketParameters
    {
        public IDictionary<string, string> RequestHeaders { get; set; }

        public CookieContainer CookierContainer { get; set; }
    }
}
