using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting.Events
{
    public delegate Task<bool> HttpWebSocketClientAuthenticationCallback();
}
