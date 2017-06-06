using System;

namespace MQTTnet.Core.Client
{
    public static class MqttClientOptionsExtensions
    {
        public static int GetPort(this MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.Port.HasValue)
            {
                return options.Port.Value;
            }

            return !options.TlsOptions.UseTls ? 1883 : 8883;
        }
    }
}
