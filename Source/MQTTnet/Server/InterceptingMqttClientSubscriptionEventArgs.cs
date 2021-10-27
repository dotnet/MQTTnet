using System;
using System.Collections.Generic;
using System.Threading;

namespace MQTTnet.Server
{
    public sealed class InterceptingMqttClientSubscriptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets or sets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; internal set; }
        
        /// <summary>
        /// Gets or sets whether the broker should create an internal subscription for the client.
        /// The broker can also avoid this and return "success" to the client.
        /// This feature allows using the MQTT Broker as the Frontend and another system as the backend.
        /// </summary>
        public bool ProcessSubscription { get; set; } = true;
       
        /// <summary>
        /// Gets or sets whether the broker should close the client connection.
        /// </summary>
        public bool CloseConnection { get; set; }
        
        /// <summary>
        /// Gets the current client session.
        /// </summary>
        public IMqttSessionStatus Session { get; internal set; }

        /// <summary>
        /// Gets the response which will be sent to the client via the SUBACK packet.
        /// </summary>
        public MqttSubscribeResponse Response { get; } = new MqttSubscribeResponse();
        
        /// <summary>
        /// Gets the cancellation token which can indicate that the client connection gets down.
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}