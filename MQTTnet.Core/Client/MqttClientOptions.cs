using System;

namespace MQTTnet.Core.Client
{
    public class MqttClientOptions
    {
        public string Server { get; set; }

        public int Port { get; set; } = 1883;

        public string UserName { get; set; }

        public string Password { get; set; }

        public string ClientId { get; set; } = Guid.NewGuid().ToString().Replace("-", string.Empty);

        public bool CleanSession { get; set; } = true;

        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Use SSL to communicate with the MQTT server.
        /// </summary>
        /// <remarks>Setting this value to <c>true</c> will also set <see cref="Port"/> to <c>8883</c> if its value was <c>1883</c> (not set).</remarks>
        public bool UseSSL
        {
            get => _useSSL;
            set
            {
                // Automatically set the port to the MQTT SSL port (8883) if it wasn't set already
                if (value && Port == 1883)
                    Port = 8883;

                _useSSL = value;
            }
        }

        private bool _useSSL;
    }
}
