﻿using System;

namespace MQTTnet.Core.Diagnostics
{
    public static class MqttNetGlobalLog
    {
        public static event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

        public static bool HasListeners => LogMessagePublished != null;

        public static void Publish(MqttNetLogMessage logMessage)
        {
            if (logMessage == null) throw new ArgumentNullException(nameof(logMessage));

            LogMessagePublished?.Invoke(null, new MqttNetLogMessagePublishedEventArgs(logMessage));
        }
    }
}
