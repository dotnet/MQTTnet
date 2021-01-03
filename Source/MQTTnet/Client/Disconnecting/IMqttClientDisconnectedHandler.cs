using System.Threading.Tasks;

namespace MQTTnet.Client.Disconnecting
{
    public interface IMqttClientDisconnectedHandler
    {
        /// <summary>
        /// Handles the events when a client gets disconnected.
        /// </summary>
        /// <param name="eventArgs">The event args.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs);
    }
}
