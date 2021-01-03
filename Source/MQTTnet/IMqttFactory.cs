using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet
{
    public interface IMqttFactory : IMqttClientFactory, IMqttServerFactory
    {
        /// <summary>
        /// Gets the default logger.
        /// </summary>
        IMqttNetLogger DefaultLogger { get; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        IDictionary<object, object> Properties { get; }
    }
}
