// If using the MQTT client PubSubClient it must be ensured  that the request topic for each method is subscribed like the following.
_mqttClient.subscribe("MQTTnet.RPC/+/ping");
_mqttClient.subscribe("MQTTnet.RPC/+/do_something");

// It is not allowed to change the structure of the topic. Otherwise RPC will not work. So method names can be separated using
// an _ or . but no +, # or /. If it is required to distinguish between devices own rules can be defined like the following.
_mqttClient.subscribe("MQTTnet.RPC/+/deviceA.ping");
_mqttClient.subscribe("MQTTnet.RPC/+/deviceB.ping");
_mqttClient.subscribe("MQTTnet.RPC/+/deviceC.getTemperature");

// Within the callback of the MQTT client the topic must be checked if it belongs to MQTTnet RPC. The following code shows one
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