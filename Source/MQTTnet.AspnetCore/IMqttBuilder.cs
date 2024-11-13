using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.AspNetCore
{
    public interface IMqttBuilder
    {
        IServiceCollection Services { get; }
    }
}
