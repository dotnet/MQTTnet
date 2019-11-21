using MQTTnet.Packets;

namespace MQTTnet.Server.ExtendedAuthenticationExchange
{
	public interface IMqttExtendedServerAuthenticationExchangeHandler
	{
		MqttBasePacket HandleClientPackage(MqttAuthPacket authPacketUpdate);

		MqttAuthPacket CreateAuthPacket(MqttConnectPacket connectPacketProperties);
	}
}