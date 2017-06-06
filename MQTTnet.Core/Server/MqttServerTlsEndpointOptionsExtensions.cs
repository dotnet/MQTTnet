using System;

namespace MQTTnet.Core.Server
{
    public static class MqttServerTlsEndpointOptionsExtensions
    {
        public static int GetPort(this DefaultEndpointOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!options.Port.HasValue)
            {
                return 1883;
            }

            return options.Port.Value;
        }
    }
}
