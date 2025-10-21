// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.TestApp;

public static class MqttNetConsoleLogger
{
    static readonly object Lock = new();

    public static void ForwardToConsole(MqttNetEventLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogMessagePublished -= PrintToConsole;
        logger.LogMessagePublished += PrintToConsole;
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

    static void PrintToConsole(object sender, MqttNetLogMessagePublishedEventArgs e)
    {
        var output = new StringBuilder();
#pragma warning disable CA1305
        output.AppendLine($">> [{e.LogMessage.Timestamp:O}] [{e.LogMessage.ThreadId}] [{e.LogMessage.Source}] [{e.LogMessage.Level}]: {e.LogMessage.Message}");
#pragma warning restore CA1305
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