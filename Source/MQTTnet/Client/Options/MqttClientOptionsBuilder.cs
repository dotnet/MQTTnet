using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQTTnet.Client.Options
{
    public class MqttClientOptionsBuilder
    {
        readonly MqttClientOptions _options = new MqttClientOptions();

        MqttClientTcpOptions _tcpOptions;
        MqttClientWebSocketOptions _webSocketOptions;
        MqttClientOptionsBuilderTlsParameters _tlsParameters;
        MqttClientWebSocketProxyOptions _proxyOptions;

        /// <summary>
        /// Adds the protocol version to the MQTT client options.
        /// </summary>
        /// <param name="value">The protocol version.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithProtocolVersion(MqttProtocolVersion value)
        {
            if (value == MqttProtocolVersion.Unknown)
            {
                throw new ArgumentException("Protocol version is invalid.");
            }

            _options.ProtocolVersion = value;
            return this;
        }

        /// <summary>
        /// Adds the communication timeout to the MQTT client options.
        /// </summary>
        /// <param name="value">The timeout.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithCommunicationTimeout(TimeSpan value)
        {
            _options.CommunicationTimeout = value;
            return this;
        }

        /// <summary>
        /// Adds the clean session flag to the MQTT client options.
        /// </summary>
        /// <param name="value">The clean session flag.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithCleanSession(bool value = true)
        {
            _options.CleanSession = value;
            return this;
        }

        /// <summary>
        /// Adds the keep alive send interval to the MQTT client options.
        /// </summary>
        /// <param name="value">The keep alive interval.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        [Obsolete("This method is no longer supported. The client will send ping requests just before the keep alive interval is going to elapse. As per MQTT RFC the serve has to wait 1.5 times the interval so we don't need this anymore.")]
        public MqttClientOptionsBuilder WithKeepAliveSendInterval(TimeSpan value)
        {
            return this;
        }

        /// <summary>
        /// Adds no keep alive period to the MQTT client options.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithNoKeepAlive()
        {
            return WithKeepAlivePeriod(TimeSpan.Zero);
        }

        /// <summary>
        /// Adds the keep alive period to the MQTT client options.
        /// </summary>
        /// <param name="value">The keep alive period.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithKeepAlivePeriod(TimeSpan value)
        {
            _options.KeepAlivePeriod = value;
            return this;
        }

        /// <summary>
        /// Adds the client identifier to the MQTT client options.
        /// </summary>
        /// <param name="value">The client identifier.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithClientId(string value)
        {
            _options.ClientId = value;
            return this;
        }

        /// <summary>
        /// Adds the will message to the MQTT client options.
        /// </summary>
        /// <param name="value">The will message.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithWillMessage(MqttApplicationMessage value)
        {
            _options.WillMessage = value;
            return this;
        }

        /// <summary>
        /// Adds the authentication to the MQTT client options.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="method">The authentication method.</param>
        /// <param name="data">The data.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithAuthentication(string method, byte[] data)
        {
            _options.AuthenticationMethod = method;
            _options.AuthenticationData = data;
            return this;
        }

        /// <summary>
        /// Adds the will delay interval to the MQTT client options.
        /// </summary>
        /// <param name="willDelayInterval">The will delay interval.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithWillDelayInterval(uint? willDelayInterval)
        {
            _options.WillDelayInterval = willDelayInterval;
            return this;
        }

        /// <summary>
        /// Adds the topic alias maximum to the MQTT client options.
        /// </summary>
        /// <param name="topicAliasMaximum">The topic alias maximum.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithTopicAliasMaximum(ushort? topicAliasMaximum)
        {
            _options.TopicAliasMaximum = topicAliasMaximum;
            return this;
        }

        /// <summary>
        /// Adds the maximum packet size to the MQTT client options.
        /// </summary>
        /// <param name="maximumPacketSize">The maximum packet size.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithMaximumPacketSize(uint? maximumPacketSize)
        {
            _options.MaximumPacketSize = maximumPacketSize;
            return this;
        }

        /// <summary>
        /// Adds the receive maximum to the MQTT client options.
        /// </summary>
        /// <param name="receiveMaximum">The receive maximum.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithReceiveMaximum(ushort? receiveMaximum)
        {
            _options.ReceiveMaximum = receiveMaximum;
            return this;
        }

        /// <summary>
        /// Adds the request problem information to the MQTT client options.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="requestProblemInformation">The request problem information.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithRequestProblemInformation(bool? requestProblemInformation = true)
        {
            _options.RequestProblemInformation = requestProblemInformation;
            return this;
        }

        /// <summary>
        /// Adds the request response information to the MQTT client options.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="requestResponseInformation">The request response information.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithRequestResponseInformation(bool? requestResponseInformation = true)
        {
            _options.RequestResponseInformation = requestResponseInformation;
            return this;
        }

        /// <summary>
        /// Adds the session expiry interval to the MQTT client options.
        /// </summary>
        /// <param name="sessionExpiryInterval">The session expiry interval.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithSessionExpiryInterval(uint? sessionExpiryInterval)
        {
            _options.SessionExpiryInterval = sessionExpiryInterval;
            return this;
        }

        /// <summary>
        /// Adds the user property to the MQTT client options.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="name">The user property name.</param>
        /// <param name="value">The user property value.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithUserProperty(string name, string value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (value is null) throw new ArgumentNullException(nameof(value));

            if (_options.UserProperties == null)
            {
                _options.UserProperties = new List<MqttUserProperty>();
            }

            _options.UserProperties.Add(new MqttUserProperty(name, value));
            return this;
        }

        /// <summary>
        /// Adds the credentials to the MQTT client options.
        /// </summary>
        /// <param name="username">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithCredentials(string username, string password = null)
        {
            byte[] passwordBuffer = null;

            if (password != null)
            {
                passwordBuffer = Encoding.UTF8.GetBytes(password);
            }

            return WithCredentials(username, passwordBuffer);
        }

        /// <summary>
        /// Adds the credentials to the MQTT client options.
        /// </summary>
        /// <param name="username">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithCredentials(string username, byte[] password = null)
        {
            _options.Credentials = new MqttClientCredentials
            {
                Username = username,
                Password = password
            };

            return this;
        }

        /// <summary>
        /// Adds the credentials to the MQTT client options.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithCredentials(IMqttClientCredentials credentials)
        {
            _options.Credentials = credentials;

            return this;
        }

        /// <summary>
        /// Adds a extended authentication exchange handler to the MQTT client options.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler handler)
        {
            _options.ExtendedAuthenticationExchangeHandler = handler;
            return this;
        }

        /// <summary>
        /// Adds a TCP server connection to the MQTT client options.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="port">The port.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithTcpServer(string server, int? port = null)
        {
            _tcpOptions = new MqttClientTcpOptions
            {
                Server = server,
                Port = port
            };

            return this;
        }

        /// <summary>
        /// Adds a TCP server connection to the MQTT client options.
        /// </summary>
        /// <param name="optionsBuilder">The options builder.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        // TODO: Consider creating _MqttClientTcpOptionsBuilder_ as overload.
        public MqttClientOptionsBuilder WithTcpServer(Action<MqttClientTcpOptions> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            _tcpOptions = new MqttClientTcpOptions();
            optionsBuilder.Invoke(_tcpOptions);

            return this;
        }

        /// <summary>
        /// Adds a proxy connection to the MQTT client options.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="username">The user name.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="bypassOnLocal">The bypass on local flag.</param>
        /// <param name="bypassList">The bypass list.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithProxy(string address, string username = null, string password = null, string domain = null, bool bypassOnLocal = false, string[] bypassList = null)
        {
            _proxyOptions = new MqttClientWebSocketProxyOptions
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

        /// <summary>
        /// Adds a proxy connection to the MQTT client options.
        /// </summary>
        /// <param name="optionsBuilder">The options builder.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithProxy(Action<MqttClientWebSocketProxyOptions> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            _proxyOptions = new MqttClientWebSocketProxyOptions();
            optionsBuilder(_proxyOptions);
            return this;
        }

        /// <summary>
        /// Adds a web socket server to the MQTT client options.
        /// </summary>
        /// <param name="uri">The uri.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithWebSocketServer(string uri, MqttClientOptionsBuilderWebSocketParameters parameters = null)
        {
            _webSocketOptions = new MqttClientWebSocketOptions
            {
                Uri = uri,
                RequestHeaders = parameters?.RequestHeaders,
                CookieContainer = parameters?.CookieContainer
            };

            return this;
        }

        /// <summary>
        /// Adds a web socket server to the MQTT client options.
        /// </summary>
        /// <param name="optionsBuilder">The options builder.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithWebSocketServer(Action<MqttClientWebSocketOptions> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            _webSocketOptions = new MqttClientWebSocketOptions();
            optionsBuilder.Invoke(_webSocketOptions);

            return this;
        }

        /// <summary>
        /// Adds TLS to the MQTT client options.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithTls(MqttClientOptionsBuilderTlsParameters parameters)
        {
            _tlsParameters = parameters;
            return this;
        }

        /// <summary>
        /// Adds TLS to the MQTT client options.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithTls()
        {
            return WithTls(new MqttClientOptionsBuilderTlsParameters { UseTls = true });
        }

        /// <summary>
        /// Adds TLS to the MQTT client options.
        /// </summary>
        /// <param name="optionsBuilder">The options builder.</param>
        /// <returns>A new instance of the <see cref="MqttClientOptionsBuilder"/> class.</returns>
        public MqttClientOptionsBuilder WithTls(Action<MqttClientOptionsBuilderTlsParameters> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            _tlsParameters = new MqttClientOptionsBuilderTlsParameters();
            optionsBuilder(_tlsParameters);
            return this;
        }

        /// <summary>
        /// Builds the MQTT client options.
        /// </summary>
        /// <returns>The <see cref="IMqttClientOptions"/>.</returns>
        public IMqttClientOptions Build()
        {
            if (_tcpOptions == null && _webSocketOptions == null)
            {
                throw new InvalidOperationException("A channel must be set.");
            }

            if (_tlsParameters != null)
            {
                if (_tlsParameters?.UseTls == true)
                {
                    var tlsOptions = new MqttClientTlsOptions
                    {
                        UseTls = true,
                        SslProtocol = _tlsParameters.SslProtocol,
                        AllowUntrustedCertificates = _tlsParameters.AllowUntrustedCertificates,
#if WINDOWS_UWP
                        Certificates = _tlsParameters.Certificates?.Select(c => c.ToArray()).ToList(),
#else
                        Certificates = _tlsParameters.Certificates?.ToList(),
#endif
#pragma warning disable CS0618 // Type or member is obsolete
                        CertificateValidationCallback = _tlsParameters.CertificateValidationCallback,
#pragma warning restore CS0618 // Type or member is obsolete
#if NETCOREAPP3_1
                        ApplicationProtocols = _tlsParameters.ApplicationProtocols,
#endif
                        CertificateValidationHandler = _tlsParameters.CertificateValidationHandler,
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

            if (_proxyOptions != null)
            {
                if (_webSocketOptions == null)
                {
                    throw new InvalidOperationException("Proxies are only supported for WebSocket connections.");
                }

                _webSocketOptions.ProxyOptions = _proxyOptions;
            }

            _options.ChannelOptions = (IMqttClientChannelOptions)_tcpOptions ?? _webSocketOptions;

            return _options;
        }
    }
}
