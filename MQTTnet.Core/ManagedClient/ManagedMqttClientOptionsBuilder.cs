using System;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.ManagedClient
{
    public class ManagedMqttClientOptionsBuilder
    {
        private readonly ManagedMqttClientOptions _options = new ManagedMqttClientOptions();

        public ManagedMqttClientOptionsBuilder WithAutoReconnectDelay(TimeSpan value)
        {
            _options.AutoReconnectDelay = value;
            return this;
        }

        public ManagedMqttClientOptionsBuilder WithStorage(IManagedMqttClientStorage value)
        {
            _options.Storage = value;
            return this;
        }

        public ManagedMqttClientOptionsBuilder WithClientOptions(IMqttClientOptions value)
        {
            _options.ClientOptions = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        public ManagedMqttClientOptionsBuilder WithClientOptions(Action<MqttClientOptionsBuilder> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var builder = new MqttClientOptionsBuilder();
            options(builder);
            _options.ClientOptions = builder.Build();

            return this;
        }

        public ManagedMqttClientOptions Build()
        {
            if (_options.ClientOptions == null)
            {
                throw new InvalidOperationException("The ClientOptions cannot be null.");
            }

            return _options;
        }
    }
}
