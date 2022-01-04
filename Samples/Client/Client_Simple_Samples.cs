using MQTTnet.Client;

namespace MQTTnet.Samples.Client;

public static class Client_Simple_Samples
{
    public static async Task Connect_Client()
    {
        /*
         * This sample creates a simple MQTT client and connects to a public broker.
         * Make sure that the client gets disposed after using it.
         */

        var mqttFactory = new MqttFactory();

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");
        }
    }
}