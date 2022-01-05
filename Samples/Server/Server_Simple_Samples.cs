using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_Simple_Samples
{
    public static async Task Run_Minimal_Server()
    {
        /*
         * This sample starts a simple MQTT server which will accept any TCP connection.
         */
        
        var mqttFactory = new MqttFactory();

        // The port for the default endpoint is 1883.
        // The default endpoint is NOT encrypted!
        // Use the builder classes where possible.
        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .Build();
        
        // The port can be changed using the following API (not used in this example).
        new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1234)
            .Build();
        
        using (var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions))
        {
            await mqttServer.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

            // Stop and dispose the MQTT server if it is no longer needed!
            await mqttServer.StopAsync();
        }
    }

    public static async Task Validating_Connections()
    {
        /*
         * This sample starts a simple MQTT server which will check for valid credentials and client ID.
         *
         * See _Run_Minimal_Server_ for more information.
         */

        var mqttFactory = new MqttFactory();

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .Build();

        using (var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions))
        {
            // Setup connection validation before starting the server so that there is 
            // no change to connect without valid credentials.
            mqttServer.ValidatingConnectionAsync += e =>
            {
                if (e.ClientId != "ValidClientId")
                {
                    e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                }

                if (e.Username != "ValidUser")
                {
                    e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                }

                if (e.Password != "SecretPassword")
                {
                    e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                }

                return Task.CompletedTask;
            };

            await mqttServer.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

            await mqttServer.StopAsync();
        }
    }
}