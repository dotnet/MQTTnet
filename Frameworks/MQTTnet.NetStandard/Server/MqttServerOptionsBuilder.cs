using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttServerOptionsBuilder
    {
        private readonly MqttServerOptions _options = new MqttServerOptions();

        public MqttServerOptionsBuilder WithConnectionBacklog(int value)
        {
            _options.ConnectionBacklog = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultCommunicationTimeout(TimeSpan value)
        {
            _options.DefaultCommunicationTimeout = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpointPort(int value)
        {
            _options.DefaultEndpointOptions.Port = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpoint()
        {
            _options.DefaultEndpointOptions.IsEnabled = true;
            return this;
        }

        public MqttServerOptionsBuilder WithoutDefaultEndpoint()
        {
            _options.DefaultEndpointOptions.IsEnabled = false;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptedEndpoint()
        {
            _options.TlsEndpointOptions.IsEnabled = true;
            return this;
        }

        public MqttServerOptionsBuilder WithoutEncryptedEndpoint()
        {
            _options.TlsEndpointOptions.IsEnabled = false;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptionCertificate(byte[] value)
        {
            _options.TlsEndpointOptions.Certificate = value;
            return this;
        }

        public MqttServerOptionsBuilder WithStorage(IMqttServerStorage value)
        {
            _options.Storage = value;
            return this;
        }

        public MqttServerOptionsBuilder WithConnectionValidator(Func<MqttConnectPacket, MqttConnectReturnCode> value)
        {
            _options.ConnectionValidator = value;
            return this;
        }

        public MqttServerOptionsBuilder WithApplicationMessageInterceptor(Action<MqttApplicationMessageInterceptorContext> value)
        {
            _options.ApplicationMessageInterceptor = value;
            return this;
        }

        public MqttServerOptionsBuilder WithSubscriptionInterceptor(Action<MqttSubscriptionInterceptorContext> value)
        {
            _options.SubscriptionInterceptor = value;
            return this;
        }

        public MqttServerOptions Build()
        {
            return _options;
        }
    }
}
