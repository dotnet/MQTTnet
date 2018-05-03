using System;
using System.Linq;
using MQTTnet.Serializer;

namespace MQTTnet.Client
{
    public class MqttClientOptionsBuilder
    {
        private readonly MqttClientOptions _options = new MqttClientOptions();
        private MqttClientTcpOptions _tcpOptions;
        private MqttClientWebSocketOptions _webSocketOptions;

        private MqttClientTlsOptions _tlsOptions;

        public MqttClientOptionsBuilder WithProtocolVersion(MqttProtocolVersion value)
        {
            _options.ProtocolVersion = value;
            return this;
        }

        public MqttClientOptionsBuilder WithCommunicationTimeout(TimeSpan value)
        {
            _options.CommunicationTimeout = value;
            return this;
        }

        public MqttClientOptionsBuilder WithCleanSession(bool value = true)
        {
            _options.CleanSession = value;
            return this;
        }

        public MqttClientOptionsBuilder WithKeepAlivePeriod(TimeSpan value)
        {
            _options.KeepAlivePeriod = value;
            return this;
        }

        public MqttClientOptionsBuilder WithClientId(string value)
        {
            _options.ClientId = value;
            return this;
        }

        public MqttClientOptionsBuilder WithWillMessage(MqttApplicationMessage value)
        {
            _options.WillMessage = value;
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
                Certificates = certificates?.ToList()
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

        public MqttClientOptionsBuilder WithReceivedApplicationMessageProcessingMode(
            MqttReceivedApplicationMessageProcessingMode mode)
        {
            _options.ReceivedApplicationMessageProcessingMode = mode;
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
                    _tcpOptions.TlsOptions = _tlsOptions;
                }
                else if (_webSocketOptions != null)
                {
                    _webSocketOptions.TlsOptions = _tlsOptions;
                }
            }

            _options.ChannelOptions = (IMqttClientChannelOptions)_tcpOptions ?? _webSocketOptions;

            return _options;
        }
    }
}
