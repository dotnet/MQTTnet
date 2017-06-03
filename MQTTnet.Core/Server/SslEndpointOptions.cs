namespace MQTTnet.Core.Server
{
    public sealed class SslEndpointOptions
    {
        public bool IsEnabled { get; set; }

        public int? Port { get; set; }

        public byte[] Certificate { get; set; }
    }
}
