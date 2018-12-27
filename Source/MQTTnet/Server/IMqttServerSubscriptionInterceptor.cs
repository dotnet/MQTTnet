using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerSubscriptionInterceptor
    {
        Task InterceptSubscriptionAsync(MqttSubscriptionInterceptorContext context);
    }
}
