using System;
using System.Text;
using MQTTnet.Diagnostics;

namespace MQTTnet.TestApp.NetCore
{
    public static class MqttNetConsoleLogger
    {
        private static readonly object Lock = new object();

        public static void ForwardToConsole()
        {
            MqttNetGlobalLogger.LogMessagePublished -= PrintToConsole;
            MqttNetGlobalLogger.LogMessagePublished += PrintToConsole;
        }

        public static void PrintToConsole(string message, ConsoleColor color)
        {
            lock (Lock)
            {
                var backupColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(message);
                Console.ForegroundColor = backupColor;
            }
        }

        private static void PrintToConsole(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            var output = new StringBuilder();
            output.AppendLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
            if (e.TraceMessage.Exception != null)
            {
                output.AppendLine(e.TraceMessage.Exception.ToString());
            }

            var color = ConsoleColor.Red;
            switch (e.TraceMessage.Level)
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