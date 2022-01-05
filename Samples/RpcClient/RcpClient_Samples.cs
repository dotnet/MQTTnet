using MQTTnet.Client;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Protocol;

namespace MQTTnet.Samples.RpcClient;

public static class RcpClient_Samples
{
    /*
     * The extension MQTTnet.Extensions.Rpc (available as nuget) allows sending a request and waiting for the matching reply.
     * This is done via defining a pattern which uses the topic to correlate the request and the response.
     * From client usage it is possible to define a timeout.
     */
    
    public static async Task Send_Request()
    {
        var mqttFactory = new MqttFactory();
        
        // The RPC client is an addon for the existing client. So we need a regular client
        // which is wrapped later.
        
        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.hivemq.com")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions);
            
            using (var mqttRpcClient = mqttFactory.CreateMqttRpcClient(mqttClient))
            {
                // Access to a fully featured application message is not supported for RCP calls!
                // The method will throw an exception when the response was not received in time.
                await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(2), "ping", "", MqttQualityOfServiceLevel.AtMostOnce);
            }
            
            Console.WriteLine("The RPC call was successful.");
        }
    }
    
    /*
     * The device must respond to the request using the correct topic. The following C code shows how a
     * smart device like an ESP8266 must respond to the above sample.
     *
        // If using the MQTT client PubSubClient it must be ensured 
        // that the request topic for each method is subscribed like the following.
        mqttClient.subscribe("MQTTnet.RPC/+/ping");
        mqttClient.subscribe("MQTTnet.RPC/+/do_something");

        // It is not allowed to change the structure of the topic.
        // Otherwise RPC will not work.
        // So method names can be separated using an _ or . but no +, # or /.
        // If it is required to distinguish between devices
        // own rules can be defined like the following:
        mqttClient.subscribe("MQTTnet.RPC/+/deviceA.ping");
        mqttClient.subscribe("MQTTnet.RPC/+/deviceB.ping");
        mqttClient.subscribe("MQTTnet.RPC/+/deviceC.getTemperature");

        // Within the callback of the MQTT client the topic must be checked
        // if it belongs to MQTTnet RPC. The following code shows one
        // possible way of doing this.
        void mqtt_Callback(char *topic, byte *payload, unsigned int payloadLength)
        {
	        String topicString = String(topic);

	        if (topicString.startsWith("MQTTnet.RPC/")) {
		        String responseTopic = topicString + String("/response");

		        if (topicString.endsWith("/deviceA.ping")) {
			        mqtt_publish(responseTopic, "pong", false);
			        return;
		        }
	        }
        }

        // Important notes:
        // ! Do not send response message with the _retain_ flag set to true.
        // ! All required data for a RPC call and the result must be placed into the payload.
     */
}