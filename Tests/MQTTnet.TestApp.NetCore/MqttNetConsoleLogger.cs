using MQTTnet.Diagnostics;
using System;
using System.Text;

namespace MQTTnet.TestApp.NetCore
{
    public static class MqttNetConsoleLogger
    {
        static readonly object _lock = new object();

        public static void ForwardToConsole(MqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            logger.LogMessagePublished -= PrintToConsole;
            logger.LogMessagePublished += PrintToConsole;
        }

        public static void PrintToConsole(string message, ConsoleColor color)
        {
            lock (_lock)
            {
                var backupColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(message);
                Console.ForegroundColor = backupColor;
            }
        }

        static void PrintToConsole(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            var output = new StringBuilder();
            output.AppendLine($">> [{e.LogMessage.Timestamp:O}] [{e.LogMessage.ThreadId}] [{e.LogMessage.Source}] [{e.LogMessage.Level}]: {e.LogMessage.Message}");
            if (e.LogMessage.Exception != null)
            {
                output.AppendLine(e.LogMessage.Exception.ToString());
            }

            var color = ConsoleColor.Red;
            switch (e.LogMessage.Level)
            {
                case MqttNetLogLevel.Error:
                    color = ConsoleColor.Red;
                    break;
                case MqttNetLogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case MqttNetLogLevel.Info:
                    color = ConsoleColor.Green;
                    break;
                case MqttNetLogLevel.Verbose:
                    color = ConsoleColor.Gray;
                    break;
            }

            PrintToConsole(output.ToString(), color);
        }
    }
}