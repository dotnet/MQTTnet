using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerClientMessageQueueInterceptor
    {
        Task InterceptClientMessageQueueEnqueueAsync(MqttClientMessageQueueInterceptorContext context);
    }
}
