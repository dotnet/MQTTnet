using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerApplicationMessageInterceptor
    {
        Task InterceptApplicationMessagePublishAsync(MqttApplicationMessageInterceptorContext context);
    }
}
