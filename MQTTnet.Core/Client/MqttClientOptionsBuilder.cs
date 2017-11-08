using System;
using System.Linq;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Client
{
    public class MqttClientOptionsBuilder
    {
        private readonly MqttClientOptions _options = new MqttClientOptions();
        private MqttClientTcpOptions _tcpOptions;
        private MqttClientWebSocketOptions _webSocketOptions;

        private MqttClientTlsOptions _tlsOptions;

        public MqttClientOptionsBuilder WithProtocolVersion(MqttProtocolVersion protocolVersion)
        {
            _options.ProtocolVersion = protocolVersion;
            return this;
        }

        public MqttClientOptionsBuilder WithCommunicationTimeout(TimeSpan communicationTimeout)
        {
            _options.CommunicationTimeout = communicationTimeout;
            return this;
        }

        public MqttClientOptionsBuilder WithCleanSession(bool value = true)
        {
            _options.CleanSession = value;
            return this;
        }

        public MqttClientOptionsBuilder WithKeepAlivePeriod(TimeSpan keepAlivePeriod)
        {
            _options.KeepAlivePeriod = keepAlivePeriod;
            return this;
        }

        public MqttClientOptionsBuilder WithClientId(string clientId)
        {
            _options.ClientId = clientId;
            return this;
        }

        public MqttClientOptionsBuilder WithWillMessage(MqttApplicationMessage applicationMessage)
        {
            _options.WillMessage = applicationMessage;
            return this;
        }

        public MqttClientOptionsBuilder WithCredentials(string username, string password = null)
        {
            _options.Credentials = new MqttClientCredentials
            {
                Username = username,
                Password = password
            };

            return this;
        }

        public MqttClientOptionsBuilder WithTcpServer(string server, int? port = null)
        {
            _tcpOptions = new MqttClientTcpOptions
            {
                Server = server,
                Port = port
            };

            return this;
        }

        public MqttClientOptionsBuilder WithWebSocketServer(string uri)
        {
            _webSocketOptions = new MqttClientWebSocketOptions
            {
                Uri = uri
            };

            return this;
        }

        public MqttClientOptionsBuilder WithTls(
            bool allowUntrustedCertificates = false,
            bool ignoreCertificateChainErrors = false,
            bool ignoreCertificateRevocationErrors = false,
            params byte[][] certificates)
        {
            _tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                AllowUntrustedCertificates = allowUntrustedCertificates,
                IgnoreCertificateChainErrors = ignoreCertificateChainErrors,
                IgnoreCertificateRevocationErrors = ignoreCertificateRevocationErrors,
                Certificates = certificates.ToList()
            };

            return this;
        }

        public MqttClientOptionsBuilder WithTls()
        {
            _tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true
            };

            return this;
        }

        public IMqttClientOptions Build()
        {
            if (_tlsOptions != null)
            {
                if (_tcpOptions == null && _webSocketOptions == null)
                {
                    throw new InvalidOperationException("A channel (TCP or WebSocket) must be set if TLS is configured.");
                }

                if (_tcpOptions != null)
                {
                    _options.ChannelOptions = _tcpOptions;
                }
                else
                {
                    _options.ChannelOptions = _webSocketOptions;
                }
            }

            _options.ChannelOptions = (IMqttClientChannelOptions)_tcpOptions ?? _webSocketOptions;

            return _options;
        }
    }
}
