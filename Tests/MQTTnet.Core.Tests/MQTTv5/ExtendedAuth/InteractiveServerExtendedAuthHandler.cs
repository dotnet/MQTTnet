using System.Collections.Generic;
using System.Text;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.ExtendedAuthenticationExchange;

namespace MQTTnet.Tests.MQTTv5.ExtendedAuth
{
	public class InteractiveServerExtendedAuthHandler : IMqttExtendedServerAuthenticationExchangeHandler
	{
		public MqttBasePacket HandleClientPackage(MqttAuthPacket authPacketUpdate,
			IDictionary<object, object> sessionItems)
		{
			if (authPacketUpdate.Properties.AuthenticationMethod != AuthMethod.InteractiveAuthName)
			{
				return null;
			}

			var secret = Encoding.UTF8.GetString(authPacketUpdate.Properties.AuthenticationData);
			if (secret == "nonce2_12345")
				return new MqttConnAckPacket
				{
					ReturnCode = MqttConnectReturnCode.ConnectionAccepted,
					ReasonCode = MqttConnectReasonCode.Success,
					Properties = new MqttConnAckPacketProperties
					{
						AuthenticationMethod = AuthMethod.InteractiveAuthName
					}
				};

			return new MqttAuthPacket
			{
				ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication,
				Properties = new MqttAuthPacketProperties
				{
					AuthenticationMethod = AuthMethod.InteractiveAuthName,
					AuthenticationData = Encoding.UTF8.GetBytes("nonce2")
				}
			};
		}

		public MqttBasePacket CreateAuthPacket(MqttConnectPacket connectPacket)
		{
			var authenticationData = connectPacket.Properties.AuthenticationData;
			return new MqttAuthPacket
			{
				ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication,
				Properties = new MqttAuthPacketProperties
				{
					AuthenticationMethod = AuthMethod.InteractiveAuthName,
					AuthenticationData = Encoding.UTF8.GetBytes("nonce")
				}
			};
		}
	}
}