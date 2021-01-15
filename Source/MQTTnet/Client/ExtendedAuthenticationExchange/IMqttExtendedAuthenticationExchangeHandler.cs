using System.Threading.Tasks;

namespace MQTTnet.Client.ExtendedAuthenticationExchange
{
    public interface IMqttExtendedAuthenticationExchangeHandler
    {
        Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context);
    }
}
