using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server.ExtendedAuthenticationExchange
{
	public interface IMqttEnhancedAuthenticationBrokerHandler
	{
		MqttBasePacket HandleAuth(MqttAuthPacket authPacket, IDictionary<object, object> sessionItems);

		MqttBasePacket StartChallenge(MqttConnectPacket connectPacket);
	}
}