#if NET452 || NET461

namespace MQTTnet.Client
{
    public class MqttClientWebSocketProxyOptions
    {
        public string Address { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Domain { get; set; }

        public bool BypassOnLocal { get; set; } = true;

        public string[] BypassList { get; set; }
    }
}

#endif