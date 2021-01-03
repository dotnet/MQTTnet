using System.Threading.Tasks;

namespace MQTTnet.Client.Connecting
{
    public interface IMqttClientConnectedHandler
    {
        /// <summary>
        /// Handles the events when a client gets connected.
        /// </summary>
        /// <param name="eventArgs">The event args.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs);
    }
}
