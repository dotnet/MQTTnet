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

        public string ClientId { get; }

        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; }

        public bool AcceptUnsubscription { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
