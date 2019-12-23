using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server.ExtendedAuthenticationExchange
{
	public interface IMqttExtendedServerAuthenticationExchangeHandler
	{
		MqttBasePacket HandleClientPackage(MqttAuthPacket authPacket, IDictionary<object, object> sessionItems);

		MqttBasePacket CreateAuthPacket(MqttConnectPacket connectPacket);
	}
}