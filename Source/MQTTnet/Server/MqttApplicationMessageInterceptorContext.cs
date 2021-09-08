using System.Collections.Generic;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageInterceptorContext
    {
        /// <summary>
        /// Gets the currently used logger.
        /// </summary>
        public IMqttNetScopedLogger Logger { get; internal set; }

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; internal set; }

        public bool AcceptPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
