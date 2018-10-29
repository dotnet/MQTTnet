using System;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttClientOptionsBuilder
    {
        private readonly ManagedMqttClientOptions _options = new ManagedMqttClientOptions();
        private MqttClientOptionsBuilder _clientOptionsBuilder;

        public ManagedMqttClientOptionsBuilder WithMaxPendingMessages(int value)
        {
            _options.MaxPendingMessages = value;
            return this;
        }

        public ManagedMqttClientOptionsBuilder WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy value)
        {
            _options.PendingMessagesOverflowStrategy = value;
            return this;
        }

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
            if (_clientOptionsBuilder != null)
            {
                throw new InvalidOperationException("Cannot use client options builder and client options at the same time.");
            }

            _options.ClientOptions = value ?? throw new ArgumentNullException(nameof(value));

            return this;
        }

        public ManagedMqttClientOptionsBuilder WithClientOptions(MqttClientOptionsBuilder builder)
        {
            if (_options.ClientOptions != null)
            {
                throw new InvalidOperationException("Cannot use client options builder and client options at the same time.");
            }

            _clientOptionsBuilder = builder;
            return this;
        }

        public ManagedMqttClientOptionsBuilder WithClientOptions(Action<MqttClientOptionsBuilder> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_clientOptionsBuilder == null)
            {
                _clientOptionsBuilder = new MqttClientOptionsBuilder();
            }

            options(_clientOptionsBuilder);
            return this;
        }

        public ManagedMqttClientOptions Build()
        {
            if (_clientOptionsBuilder != null)
            {
                _options.ClientOptions = _clientOptionsBuilder.Build();
            }

            if (_options.ClientOptions == null)
            {
                throw new InvalidOperationException("The ClientOptions cannot be null.");
            }

            return _options;
        }
    }
}
