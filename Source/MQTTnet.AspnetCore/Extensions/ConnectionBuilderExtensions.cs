using Microsoft.AspNetCore.Connections;

namespace MQTTnet.AspNetCore.Extensions
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseMqtt(this IConnectionBuilder builder)
        {
            return builder.UseConnectionHandler<MqttConnectionHandler>();
        }
    }
}
