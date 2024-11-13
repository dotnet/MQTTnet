using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.AspNetCore
{
    /// <summary>
    /// Builder of MqttServer
    /// </summary>
    public interface IMqttServerBuilder
    {
        IServiceCollection Services { get; }
    }
}
