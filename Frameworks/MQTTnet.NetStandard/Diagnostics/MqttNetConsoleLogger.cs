using System;
using System.Text;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Diagnostics
{
    public static class MqttNetConsoleLogger
    {
        private static readonly object Lock = new object();

        public static void ForwardToConsole()
        {
            MqttNetGlobalLog.LogMessagePublished -= PrintToConsole;
            MqttNetGlobalLog.LogMessagePublished += PrintToConsole;
        }

        private static void PrintToConsole(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            var output = new StringBuilder();
            output.AppendLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
            if (e.TraceMessage.Exception != null)
            {
                output.AppendLine(e.TraceMessage.Exception.ToString());
            }

            lock (Lock)
            {
                var backupColor = Console.ForegroundColor;
                switch (e.TraceMessage.Level)
                {
                    case MqttNetLogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case MqttNetLogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case MqttNetLogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case MqttNetLogLevel.Verbose:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }

                Console.Write(output);
                Console.ForegroundColor = backupColor;
            }
        }
    }
}