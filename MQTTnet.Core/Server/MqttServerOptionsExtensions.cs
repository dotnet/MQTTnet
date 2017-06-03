using System;

namespace MQTTnet.Core.Server
{
    public static class MqttServerOptionsExtensions
    {
        public static int GetSslEndpointPort(this MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!options.SslEndpointOptions.Port.HasValue)
            {
                return 8883;
            }

            return options.SslEndpointOptions.Port.Value;
        }

        public static int GetDefaultEndpointPort(this MqttServerOptions options)
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
