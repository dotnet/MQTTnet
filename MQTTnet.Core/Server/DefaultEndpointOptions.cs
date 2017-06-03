namespace MQTTnet.Core.Server
{
    public sealed class DefaultEndpointOptions
    {
        public bool IsEnabled { get; set; } = true;

        public int? Port { get; set; }
    }
}
