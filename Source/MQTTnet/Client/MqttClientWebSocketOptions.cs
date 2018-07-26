using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public class MqttClientWebSocketOptions : IMqttClientChannelOptions
    {
        public string Uri { get; set; }

        public IDictionary<string, string> RequestHeaders { get; set; }

        public ICollection<string> SubProtocols { get; set; } = new List<string> { "mqtt" };

        public CookieContainer CookieContainer { get; set; }

#if NET452 || NET461
        public MqttClientWebSocketProxyOptions MqttClientWebSocketProxy { get; set; } = new MqttClientWebSocketProxyOptions();
#endif

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public override string ToString()
        {
            return Uri;
        }
    }
}
