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
        private MqttClientOptionsBuilderTlsParameters _tlsParameters;

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

        public MqttClientOptionsBuilder WithKeepAliveSendInterval(TimeSpan value)
        {
            _options.KeepAliveSendInterval = value;
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

#if NET452 || NET461

        public MqttClientOptionsBuilder WithProxy(string address, string username = null, string password = null, string domain = null, bool bypassOnLocal = false, string[] bypassList = null)
        {
            if (_webSocketOptions == null)
            {
                throw new InvalidOperationException("A WebSocket channel must be set if MqttClientWebSocketProxy is configured.");
            }

            _webSocketOptions.ProxyOptions = new MqttClientWebSocketProxyOptions
            {
                Address = address,
                Username = username,
                Password = password,
                Domain = domain,
                BypassOnLocal = bypassOnLocal,
                BypassList = bypassList
            };

            return this;
        }
#endif

        public MqttClientOptionsBuilder WithWebSocketServer(string uri)
        {
            _webSocketOptions = new MqttClientWebSocketOptions
            {
                Uri = uri
            };

            return this;
        }

        public MqttClientOptionsBuilder WithTls(MqttClientOptionsBuilderTlsParameters parameters)
        {
            _tlsParameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            return this;
        }

        public MqttClientOptionsBuilder WithTls()
        {
            return WithTls(new MqttClientOptionsBuilderTlsParameters { UseTls = true });
        }

        [Obsolete("Use method _WithTlps_ which accepts the _MqttClientOptionsBuilderTlsParameters_.")]
        public MqttClientOptionsBuilder WithTls(
            bool allowUntrustedCertificates = false,
            bool ignoreCertificateChainErrors = false,
            bool ignoreCertificateRevocationErrors = false,
            params byte[][] certificates)
        {
            _tlsParameters = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                AllowUntrustedCertificates = allowUntrustedCertificates,
                IgnoreCertificateChainErrors = ignoreCertificateChainErrors,
                IgnoreCertificateRevocationErrors = ignoreCertificateRevocationErrors,
                Certificates = certificates?.ToList()
            };

            return this;
        }

        public IMqttClientOptions Build()
        {
            if (_tlsParameters != null)
            {
                if (_tcpOptions == null && _webSocketOptions == null)
                {
                    throw new InvalidOperationException("A channel (TCP or WebSocket) must be set if TLS is configured.");
                }

                if (_tlsParameters?.UseTls == true)
                {
                    var tlsOptions = new MqttClientTlsOptions
                    {
                        UseTls = true,
                        SslProtocol = _tlsParameters.SslProtocol,
                        AllowUntrustedCertificates = _tlsParameters.AllowUntrustedCertificates,
                        Certificates = _tlsParameters.Certificates?.Select(c => c.ToArray()).ToList(),
                        CertificateValidationCallback = _tlsParameters.CertificateValidationCallback,
                        IgnoreCertificateChainErrors = _tlsParameters.IgnoreCertificateChainErrors,
                        IgnoreCertificateRevocationErrors = _tlsParameters.IgnoreCertificateRevocationErrors
                    };

                    if (_tcpOptions != null)
                    {
                        _tcpOptions.TlsOptions = tlsOptions;
                    }
                    else if (_webSocketOptions != null)
                    {
                        _webSocketOptions.TlsOptions = tlsOptions;
                    }
                }
            }

            _options.ChannelOptions = (IMqttClientChannelOptions)_tcpOptions ?? _webSocketOptions;

            return _options;
        }
    }
}
