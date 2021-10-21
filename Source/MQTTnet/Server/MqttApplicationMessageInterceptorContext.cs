using System;
using System.Collections.Generic;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageInterceptorContext
    { 
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

        [Obsolete("Please use ProcessPublish instead.")]
        public bool AcceptPublish 
        { 
            get => ProcessPublish;
            set => ProcessPublish = value;
        }
        
        /// <summary>
        /// Gets or sets whether the publish should be processed internally.
        /// </summary>
        public bool ProcessPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
