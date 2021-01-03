using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.LowLevelClient;

namespace MQTTnet.Client
{
    public interface IMqttClientFactory
    {
        /// <summary>
        /// A method to tell the client to use the adapter factory.
        /// </summary>
        /// <param name="clientAdapterFactory">The client adapter factory.</param>
        /// <returns>A new <see cref="IMqttFactory"/>.</returns>
        IMqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory);

        /// <summary>
        /// Creates a new low level MQTT client.
        /// </summary>
        /// <returns>A new <see cref="ILowLevelMqttClient"/>.</returns>
        ILowLevelMqttClient CreateLowLevelMqttClient();

        /// <summary>
        /// Creates a new low level MQTT client.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>A new <see cref="ILowLevelMqttClient"/>.</returns>
        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger);

        /// <summary>
        /// Creates a new low level MQTT client.
        /// </summary>
        /// <param name="clientAdapterFactory">The client adapter factory.</param>
        /// <returns>A new <see cref="ILowLevelMqttClient"/>.</returns>
        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory);

        /// <summary>
        /// Creates a new low level MQTT client.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientAdapterFactory">The client adapter factory.</param>
        /// <returns>A new <see cref="ILowLevelMqttClient"/>.</returns>
        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory);

        /// <summary>
        /// Creates a new MQTT client.
        /// </summary>
        /// <returns>A new <see cref="IMqttClient"/>.</returns>
        IMqttClient CreateMqttClient();

        /// <summary>
        /// Creates a new MQTT client.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>A new <see cref="IMqttClient"/>.</returns>
        IMqttClient CreateMqttClient(IMqttNetLogger logger);

        /// <summary>
        /// Creates a new MQTT client.
        /// </summary>
        /// <param name="adapterFactory">The adapter factory.</param>
        /// <returns>A new <see cref="IMqttClient"/>.</returns>
        IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory);

        /// <summary>
        /// Creates a new MQTT client.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="adapterFactory">The adapter factory.</param>
        /// <returns>A new <see cref="IMqttClient"/>.</returns>
        IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory);
    }
}