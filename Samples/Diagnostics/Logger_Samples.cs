using System.Text;
using MQTTnet.Diagnostics;

namespace MQTTnet.Samples.Diagnostics;

public static class Logger_Samples
{
    public static async Task Use_Custom_Logger()
    {
        /*
         * This sample shows how to get logs from the library.
         *
         * ATTENTION: Only use the logger for debugging etc. The performance is heavily decreased when a logger is used.
         */
        
        // The logger ID is optional but can be set do distinguish different logger instances.
        var mqttEventLogger = new MqttNetEventLogger("MyCustomLogger");

        mqttEventLogger.LogMessagePublished += (sender, args) =>
        {
            var output = new StringBuilder();
            output.AppendLine($">> [{args.LogMessage.Timestamp:O}] [{args.LogMessage.ThreadId}] [{args.LogMessage.Source}] [{args.LogMessage.Level}]: {args.LogMessage.Message}");
            if (args.LogMessage.Exception != null)
            {
                output.AppendLine(args.LogMessage.Exception.ToString());
            }
            
            Console.Write(output);
        };
        
        var mqttFactory = new MqttFactory(mqttEventLogger);

        var mqttClientOptions = mqttFactory.CreateClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com")
            .Build();
        
        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            
            Console.WriteLine("MQTT client is connected.");

            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder()
                .Build();
            
            await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        }
    }
}