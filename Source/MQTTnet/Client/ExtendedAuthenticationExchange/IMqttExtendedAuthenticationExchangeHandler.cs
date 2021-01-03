using System.Threading.Tasks;

namespace MQTTnet.Client.ExtendedAuthenticationExchange
{
    public interface IMqttExtendedAuthenticationExchangeHandler
    {
        /// <summary>
        /// Handles the authentication request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context);
    }
}
