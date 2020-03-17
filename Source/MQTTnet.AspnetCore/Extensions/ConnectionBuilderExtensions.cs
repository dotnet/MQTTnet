using Microsoft.AspNetCore.Connections;

namespace MQTTnet.AspNetCore
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseMqtt(this IConnectionBuilder builder)
        {
            return builder.UseConnectionHandler<MqttConnectionHandler>();
        }
    }
}
