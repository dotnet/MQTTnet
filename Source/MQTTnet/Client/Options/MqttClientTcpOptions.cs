using System.Net.Sockets;

namespace MQTTnet.Client.Options
{
    public class MqttClientTcpOptions : IMqttClientChannelOptions
    {
        public string Server { get; set; }

        public int? Port { get; set; }

        public int BufferSize { get; set; } = 65536;

        public bool? DualMode { get; set; }

        public bool NoDelay { get; set; } = true;

        public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public override string ToString()
        {
            return Server + ":" + this.GetPort();
        }
    }
}
