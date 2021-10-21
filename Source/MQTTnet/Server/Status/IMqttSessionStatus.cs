using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server.Status
{
    public interface IMqttSessionStatus
    {
        /// <summary>
        /// Fired when this session is being deleted.
        /// </summary>
        event EventHandler Deleted;
        
        /// <summary>
        /// Gets or sets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Gets the count of messages which are not yet sent to the client but already queued.
        /// </summary>
        long PendingApplicationMessagesCount { get; }

        /// <summary>
        /// This items can be used by the library user in order to store custom information.
        /// </summary>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Enqueues a new application message.
        /// </summary>
        /// <param name="applicationMessage">The application message.</param>
        /// <returns>A task.</returns>
        Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage);
        
        /// <summary>
        /// Delivers a new application message.
        /// </summary>
        /// <param name="applicationMessage">The application message.</param>
        /// <returns>A task.</returns>
        Task DeliverApplicationMessageAsync(MqttApplicationMessage applicationMessage);
        
        /// <summary>
        /// Clears the entire queue with pending application messages.
        /// </summary>
        /// <returns>A task.</returns>
        Task ClearApplicationMessagesQueueAsync();

        [Obsolete("Please use _ClearApplicationMessagesQueueAsync_ instead. This will be removed soon.")]
        Task ClearPendingApplicationMessagesAsync();
        
        Task DeleteAsync();
    }
}