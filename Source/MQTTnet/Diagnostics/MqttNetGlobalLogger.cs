using System;

namespace MQTTnet.Diagnostics
{
    [Obsolete("Please pass an instance of the IMqttNetLogger to the factory/server or client. The global logger will be deleted in the future.")]
    public static class MqttNetGlobalLogger
    {
        public static event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public static bool HasListeners => LogMessagePublished != null;

        public static void Publish(MqttNetLogMessage logMessage)
        {
            LogMessagePublished?.Invoke(null, new MqttNetLogMessagePublishedEventArgs(logMessage));
        }
    }
}
