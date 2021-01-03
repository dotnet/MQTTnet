using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public interface IMqttServerFactory
    {
        /// <summary>
        /// Gets the default server adapters.
        /// </summary>
        IList<Func<IMqttFactory, IMqttServerAdapter>> DefaultServerAdapters { get; }

        /// <summary>
        /// Creates a new default MQTT server.
        /// </summary>
        /// <returns>A new <see cref="IMqttServer"/>.</returns>
        IMqttServer CreateMqttServer();

        /// <summary>
        /// Creates a new default MQTT server.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>A new <see cref="IMqttServer"/>.</returns>
        IMqttServer CreateMqttServer(IMqttNetLogger logger);

        /// <summary>
        /// Creates a new default MQTT server.
        /// </summary>
        /// <param name="adapters">The adapters.</param>
        /// <returns>A new <see cref="IMqttServer"/>.</returns>
        IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters);

        /// <summary>
        /// Creates a new default MQTT server.
        /// </summary>
        /// <param name="adapters">The adapters.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A new <see cref="IMqttServer"/>.</returns>
        IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger);
    }
}