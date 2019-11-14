using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServerUnsubscriptionInterceptor
    {
        Task InterceptUnsubscriptionAsync(MqttUnsubscriptionInterceptorContext context);
    }
}
