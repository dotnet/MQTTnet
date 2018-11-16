using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Core.Tests
{
    public static class TestServerExtensions
    {
        /// <summary>
        /// publishes a message with a client and waits in the server until a message with the same topic is received
        /// </summary>
        /// <returns></returns>
        public static async Task PublishAndWaitForAsync(this IMqttClient client, IMqttServer server, MqttApplicationMessage message)
        {
            var tcs = new TaskCompletionSource<object>();

            void Handler(object sender, MqttApplicationMessageReceivedEventArgs args)
            {
                if (args.ApplicationMessage.Topic == message.Topic)
                {
                    tcs.SetResult(true);
                }
            }

            server.ApplicationMessageReceived += Handler;

            try
            {
                await client.PublishAsync(message).ConfigureAwait(false);
                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                server.ApplicationMessageReceived -= Handler;
            }
        }
    }
}
