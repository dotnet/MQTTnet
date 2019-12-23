using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.MQTTv5.ExtendedAuth
{
    public class InteractiveClientExtendedAuthHandler : IMqttExtendedAuthenticationExchangeHandler
    {
        public Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context)
        {
            if (context.AuthenticationMethod != AuthMethod.InteractiveAuthName) return Task.CompletedTask;

            var nonce = context.AuthenticationData;
            var nonceString = Encoding.UTF8.GetString(nonce);
            if (nonceString == "nonce2")
            {
                return context.Client.SendExtendedAuthenticationExchangeDataAsync(
                    new MqttExtendedAuthenticationExchangeData
                    {
                        AuthenticationData = Encoding.UTF8.GetBytes("nonce2_12345"),
                        ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication
                    });
            }

            return context.Client.SendExtendedAuthenticationExchangeDataAsync(new MqttExtendedAuthenticationExchangeData
            {
                AuthenticationData = Encoding.UTF8.GetBytes("nonce12345"),
                ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication
            });
        }
    }
}