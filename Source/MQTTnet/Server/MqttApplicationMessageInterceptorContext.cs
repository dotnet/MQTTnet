using System;
using System.Collections.Generic;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, IDictionary<object, object> sessionItems, IMqttNetScopedLogger logger)
        {
            ClientId = clientId;
            SessionItems = sessionItems;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the currently used logger.
        /// </summary>
        public IMqttNetScopedLogger Logger { get; }

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
