using System.Threading.Tasks;

namespace MQTTnet.Client.Receiving
{
    public interface IMqttApplicationMessageReceivedHandler
    {
        /// <summary>
        /// Handles the application message received event.
        /// </summary>
        /// <param name="eventArgs">The event args.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs);
    }
}
