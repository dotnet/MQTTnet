using System.Collections.Generic;

namespace MQTTnet.Server
{
    public class MqttUnsubscriptionInterceptorContext
    {
        public MqttUnsubscriptionInterceptorContext(string clientId, string topic, IDictionary<object, object> sessionItems)
        {
            ClientId = clientId;
            Topic = topic;
            SessionItems = sessionItems;
        }

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets or sets the MQTT topic.
        /// In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected client.
        /// The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level separator). 
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; }

        public bool AcceptUnsubscription { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
