using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;

namespace MQTTnet
{
    public interface IApplicationMessagePublisher
    {
        /// <summary>
        /// Publishes a new application message to the server.
        /// </summary>
        /// <param name="applicationMessage">The application message.</param>
        /// <param name="cancellationToken">A cancellation token to stop the task.</param>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
        Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken);
    }
}
