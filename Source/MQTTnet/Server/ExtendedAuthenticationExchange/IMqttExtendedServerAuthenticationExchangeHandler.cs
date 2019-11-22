using MQTTnet.Packets;

namespace MQTTnet.Server.ExtendedAuthenticationExchange
{
	public interface IMqttExtendedServerAuthenticationExchangeHandler
	{
		MqttBasePacket HandleClientPackage(MqttAuthPacket authPacketUpdate);

		MqttBasePacket CreateAuthPacket(MqttConnectPacket connectPacketProperties);
	}
}