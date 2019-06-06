using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.Internal
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task, IMqttNetChildLogger logger)
        {
            task?.ContinueWith(t =>
                {
                    logger.Error(t.Exception, "Unhandled exception.");
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
