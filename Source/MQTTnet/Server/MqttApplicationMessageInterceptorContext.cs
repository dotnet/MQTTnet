using System.Collections.Generic;

namespace MQTTnet.Server
{
    public class MqttApplicationMessageInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, IDictionary<object, object> sessionItems, MqttApplicationMessage applicationMessage)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage;
            SessionItems = sessionItems;
        }

        public string ClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; }

        public bool AcceptPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
