using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttExtendedAuthenticationExchangeHandler
    {
        Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context);
    }
}
