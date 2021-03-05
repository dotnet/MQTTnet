using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc
{
    public static class MqttRpcClientExtensions
    {
        public static Task<byte[]> ExecuteAsync(this IMqttRpcClient client, TimeSpan timeout, string methodName, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var buffer = Encoding.UTF8.GetBytes(payload ?? string.Empty);

            return client.ExecuteAsync(timeout, methodName, buffer, qualityOfServiceLevel);
        }
    }
}