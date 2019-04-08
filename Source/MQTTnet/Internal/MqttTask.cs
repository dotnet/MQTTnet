using MQTTnet.Client.Publishing;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    internal static class MqttTask
    {
        static MqttTask()
        {
#if NETSTANDARD2_0 || NET461 || WINDOWS_UWP
            Completed = Task.CompletedTask;
#else
            Completed = Task.FromResult(0);
#endif
        }

        public static Task Completed { get; }

        public static ValueTask<MqttClientPublishResult> PublishedSuccessfully { get; } = new ValueTask<MqttClientPublishResult>(new MqttClientPublishResult());
    }
}
