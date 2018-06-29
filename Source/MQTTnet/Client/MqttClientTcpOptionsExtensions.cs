using System;

namespace MQTTnet.Client
{
    public static class MqttClientTcpOptionsExtensions
    {
        public static int GetPort(this MqttClientTcpOptions options)
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
