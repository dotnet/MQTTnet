using System;

namespace MQTTnet.Server
{
    public static class MqttServerOptionsExtensions
    {
        public static int GetTlsEndpointPort(this IMqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!options.TlsEndpointOptions.Port.HasValue)
            {
                return 8883;
            }

            return options.TlsEndpointOptions.Port.Value;
        }

        public static int GetDefaultEndpointPort(this IMqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!options.DefaultEndpointOptions.Port.HasValue)
            {
                return 1883;
            }

            return options.DefaultEndpointOptions.Port.Value;
        }
    }
}
