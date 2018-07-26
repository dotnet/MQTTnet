using System;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
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

            _webSocketOptions.MqttClientWebSocketProxy = new MqttClientWebSocketProxyOptions
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


        public MqttClientOptionsBuilder WithTls()
        {
            return WithTls(null);
        }

        public MqttClientOptionsBuilder WithTls(Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback)
        {
            return WithTls(SslProtocols.None, certificateValidationCallback);
        }

        public MqttClientOptionsBuilder WithTls(SslProtocols sslProtocol,
            Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback = null)
        {
            return WithTls(new byte[][] { }, sslProtocol, certificateValidationCallback);
        }

        public MqttClientOptionsBuilder WithTls(byte[][] certificates,
            SslProtocols sslProtocol = SslProtocols.Tls12,
            Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback = null)
        {
            return WithTls(false, certificates, sslProtocol, certificateValidationCallback);
        }

        public MqttClientOptionsBuilder WithTls(bool ignoreCertificateRevocationErrors,
            byte[][] certificates = null,
            SslProtocols sslProtocol = SslProtocols.Tls12,
            Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback = null)
        {
            return WithTls(false, ignoreCertificateRevocationErrors, certificates, sslProtocol, certificateValidationCallback);
        }

        public MqttClientOptionsBuilder WithTls(bool ignoreCertificateChainErrors,
            bool ignoreCertificateRevocationErrors = false,
            byte[][] certificates = null,
            SslProtocols sslProtocol = SslProtocols.Tls12,
            Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback = null)
        {
            return WithTls(false, ignoreCertificateChainErrors, ignoreCertificateRevocationErrors, certificates, sslProtocol, certificateValidationCallback);
        }

        public MqttClientOptionsBuilder WithTls(
            bool allowUntrustedCertificates,
            bool ignoreCertificateChainErrors = false,
            bool ignoreCertificateRevocationErrors = false,
            byte[][] certificates = null,
            SslProtocols sslProtocol = SslProtocols.Tls12,
            Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> certificateValidationCallback = null)
        {
            _tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                AllowUntrustedCertificates = allowUntrustedCertificates,
                IgnoreCertificateChainErrors = ignoreCertificateChainErrors,
                IgnoreCertificateRevocationErrors = ignoreCertificateRevocationErrors,
                Certificates = certificates?.ToList(),
                SslProtocol = sslProtocol,
                CertificateValidationCallback = certificateValidationCallback
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
