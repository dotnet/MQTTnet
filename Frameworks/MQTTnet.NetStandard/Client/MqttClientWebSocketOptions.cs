using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public class MqttClientWebSocketOptions : IMqttClientChannelOptions
    {
        public string Uri { get; set; }

        public IDictionary<string, string> RequestHeaders { get; set; }

        public ICollection<string> SubProtocols { get; set; }

        public CookieContainer CookieContainer { get; set; }

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();
    }
}
