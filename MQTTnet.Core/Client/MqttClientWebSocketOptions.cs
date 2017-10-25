using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Core.Client
{
    public class MqttClientWebSocketOptions : BaseMqttClientOptions
    {
        public string Uri { get; set; }

        public IDictionary<string, string> RequestHeaders { get; set; }

        public ICollection<string> SubProtocols { get; set; }

        public CookieContainer CookieContainer { get; set; }
    }
}
