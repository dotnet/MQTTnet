using System.Collections.Generic;
using System.Threading;

namespace MQTTnet.Server
{
    public sealed class MqttUnsubscriptionInterceptorContext
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets or sets the MQTT topic.
        /// In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected client.
        /// The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level separator). 
        /// </summary>
        public string Topic { get; internal set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; internal set; }

        /// <summary>
        /// Gets the response which will be sent to the client via the UNSUBACK pocket.
        /// </summary>
        public MqttUnsubscribeResponse Response { get; } = new MqttUnsubscribeResponse();
        
        /// <summary>
        /// Gets or sets whether the broker should remove an internal subscription for the client.
        /// The broker can also avoid this and return "success" to the client.
        /// This feature allows using the MQTT Broker as the Frontend and another system as the backend.
        /// </summary>
        public bool ProcessUnsubscription { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether the broker should close the client connection.
        /// </summary>
        public bool CloseConnection { get; set; }
        
        /// <summary>
        /// Gets the cancellation token which can indicate that the client connection gets down.
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}
