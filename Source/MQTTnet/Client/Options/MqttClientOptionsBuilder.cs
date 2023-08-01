// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientOptionsBuilder
    {
        readonly MqttClientOptions _options = new MqttClientOptions();
        MqttClientWebSocketProxyOptions _proxyOptions;

        MqttClientTcpOptions _tcpOptions;
        MqttClientOptionsBuilderTlsParameters _tlsParameters;
        MqttClientWebSocketOptions _webSocketOptions;

        public MqttClientOptions Build()
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
                        CertificateValidationHandler = _tlsParameters.CertificateValidationHandler,
                        IgnoreCertificateChainErrors = _tlsParameters.IgnoreCertificateChainErrors,
                        IgnoreCertificateRevocationErrors = _tlsParameters.IgnoreCertificateRevocationErrors,
#if WINDOWS_UWP
                        Certificates = _tlsParameters.Certificates?.Select(c => c.ToArray()).ToList(),
#else
                        Certificates = _tlsParameters.Certificates?.ToList(),
#endif

#if NETCOREAPP3_1_OR_GREATER
                        ApplicationProtocols = _tlsParameters.ApplicationProtocols,
#endif
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

            MqttClientOptionsValidator.ThrowIfNotSupported(_options);

            return _options;
        }

        public MqttClientOptionsBuilder WithAuthentication(string method, byte[] data)
        {
            _options.AuthenticationMethod = method;
            _options.AuthenticationData = data;
            return this;
        }

        /// <summary>
        ///     Clean session is used in MQTT versions below 5.0.0. It is the same as setting "CleanStart".
        /// </summary>
        public MqttClientOptionsBuilder WithCleanSession(bool value = true)
        {
            _options.CleanSession = value;
            return this;
        }

        /// <summary>
        ///     Clean start is used in MQTT versions 5.0.0 and higher. It is the same as setting "CleanSession".
        /// </summary>
        public MqttClientOptionsBuilder WithCleanStart(bool value = true)
        {
            _options.CleanSession = value;
            return this;
        }

        public MqttClientOptionsBuilder WithClientId(string value)
        {
            _options.ClientId = value;
            return this;
        }

        public MqttClientOptionsBuilder WithConnectionUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var port = uri.IsDefaultPort ? null : (int?)uri.Port;
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                case "mqtt":
                    WithTcpServer(uri.Host, port);
                    break;

                case "mqtts":
                    WithTcpServer(uri.Host, port).WithTls();
                    break;

                case "ws":
                case "wss":
                    WithWebSocketServer(uri.ToString());
                    break;

                default:
                    throw new ArgumentException("Unexpected scheme in uri.");
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                WithCredentials(username, password);
            }

            return this;
        }

        public MqttClientOptionsBuilder WithConnectionUri(string uri)
        {
            return WithConnectionUri(new Uri(uri, UriKind.Absolute));
        }

        public MqttClientOptionsBuilder WithCredentials(string username, string password)
        {
            byte[] passwordBuffer = null;

            if (password != null)
            {
                passwordBuffer = Encoding.UTF8.GetBytes(password);
            }

            return WithCredentials(username, passwordBuffer);
        }

        public MqttClientOptionsBuilder WithCredentials(string username, byte[] password = null)
        {
            return WithCredentials(new MqttClientCredentials(username, password));
        }

        public MqttClientOptionsBuilder WithCredentials(IMqttClientCredentialsProvider credentials)
        {
            _options.Credentials = credentials;
            return this;
        }

        public MqttClientOptionsBuilder WithExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler handler)
        {
            _options.ExtendedAuthenticationExchangeHandler = handler;
            return this;
        }

        public MqttClientOptionsBuilder WithKeepAlivePeriod(TimeSpan value)
        {
            _options.KeepAlivePeriod = value;
            return this;
        }

        public MqttClientOptionsBuilder WithMaximumPacketSize(uint maximumPacketSize)
        {
            _options.MaximumPacketSize = maximumPacketSize;
            return this;
        }

        public MqttClientOptionsBuilder WithNoKeepAlive()
        {
            return WithKeepAlivePeriod(TimeSpan.Zero);
        }

        /// <summary>
        ///     Usually the MQTT packets can be send partially. This is done by using multiple TCP packets
        ///     or WebSocket frames etc. Unfortunately not all brokers (like Amazon Web Services (AWS)) do support this feature and
        ///     will close the connection when receiving such packets. If such a service is used this flag must
        ///     be set to _true_.
        /// </summary>
        public MqttClientOptionsBuilder WithoutPacketFragmentation()
        {
            _options.AllowPacketFragmentation = false;
            return this;
        }

        public MqttClientOptionsBuilder WithProtocolVersion(MqttProtocolVersion value)
        {
            if (value == MqttProtocolVersion.Unknown)
            {
                throw new ArgumentException("Protocol version is invalid.");
            }

            _options.ProtocolVersion = value;
            return this;
        }

        public MqttClientOptionsBuilder WithProxy(
            string address,
            string username = null,
            string password = null,
            string domain = null,
            bool bypassOnLocal = false,
            string[] bypassList = null)
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

        public MqttClientOptionsBuilder WithProxy(Action<MqttClientWebSocketProxyOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            _proxyOptions = new MqttClientWebSocketProxyOptions();
            optionsBuilder(_proxyOptions);
            return this;
        }

        public MqttClientOptionsBuilder WithReceiveMaximum(ushort receiveMaximum)
        {
            _options.ReceiveMaximum = receiveMaximum;
            return this;
        }

        public MqttClientOptionsBuilder WithRequestProblemInformation(bool requestProblemInformation = true)
        {
            _options.RequestProblemInformation = requestProblemInformation;
            return this;
        }

        public MqttClientOptionsBuilder WithRequestResponseInformation(bool requestResponseInformation = true)
        {
            _options.RequestResponseInformation = requestResponseInformation;
            return this;
        }

        public MqttClientOptionsBuilder WithSessionExpiryInterval(uint sessionExpiryInterval)
        {
            _options.SessionExpiryInterval = sessionExpiryInterval;
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

        public MqttClientOptionsBuilder WithTcpServer(Action<MqttClientTcpOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            _tcpOptions = new MqttClientTcpOptions();
            optionsBuilder.Invoke(_tcpOptions);

            return this;
        }

        /// <summary>
        ///     Sets the timeout which will be applied at socket level and internal operations.
        ///     The default value is the same as for sockets in .NET in general.
        /// </summary>
        public MqttClientOptionsBuilder WithTimeout(TimeSpan value)
        {
            _options.Timeout = value;
            return this;
        }

        public MqttClientOptionsBuilder WithTls(MqttClientOptionsBuilderTlsParameters parameters)
        {
            _tlsParameters = parameters;
            return this;
        }

        public MqttClientOptionsBuilder WithTls()
        {
            return WithTls(new MqttClientOptionsBuilderTlsParameters { UseTls = true });
        }

        public MqttClientOptionsBuilder WithTls(Action<MqttClientOptionsBuilderTlsParameters> optionsBuilder)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            _tlsParameters = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true
            };

            optionsBuilder(_tlsParameters);
            return this;
        }

        public MqttClientOptionsBuilder WithTopicAliasMaximum(ushort topicAliasMaximum)
        {
            _options.TopicAliasMaximum = topicAliasMaximum;
            return this;
        }

        /// <summary>
        ///     If set to true, the bridge will attempt to indicate to the remote broker that it is a bridge not an ordinary
        ///     client.
        ///     If successful, this means that loop detection will be more effective and that retained messages will be propagated
        ///     correctly.
        ///     Not all brokers support this feature so it may be necessary to set it to false if your bridge does not connect
        ///     properly.
        /// </summary>
        public MqttClientOptionsBuilder WithTryPrivate(bool tryPrivate = true)
        {
            _options.TryPrivate = true;
            return this;
        }

        public MqttClientOptionsBuilder WithUserProperty(string name, string value)
        {
            if (_options.UserProperties == null)
            {
                _options.UserProperties = new List<MqttUserProperty>();
            }

            _options.UserProperties.Add(new MqttUserProperty(name, value));
            return this;
        }

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

        public MqttClientOptionsBuilder WithWebSocketServer(Action<MqttClientWebSocketOptions> optionsBuilder)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            _webSocketOptions = new MqttClientWebSocketOptions();
            optionsBuilder.Invoke(_webSocketOptions);

            return this;
        }

        public MqttClientOptionsBuilder WithWillContentType(string willContentType)
        {
            _options.WillContentType = willContentType;
            return this;
        }

        public MqttClientOptionsBuilder WithWillCorrelationData(byte[] willCorrelationData)
        {
            _options.WillCorrelationData = willCorrelationData;
            return this;
        }

        public MqttClientOptionsBuilder WithWillDelayInterval(uint willDelayInterval)
        {
            _options.WillDelayInterval = willDelayInterval;
            return this;
        }

        public MqttClientOptionsBuilder WithWillPayload(byte[] willPayload)
        {
            _options.WillPayload = willPayload;
            return this;
        }

        public MqttClientOptionsBuilder WithWillPayload(string willPayload)
        {
            if (string.IsNullOrEmpty(willPayload))
            {
                return WithWillPayload((byte[])null);
            }

            _options.WillPayload = Encoding.UTF8.GetBytes(willPayload);
            return this;
        }

        public MqttClientOptionsBuilder WithWillPayloadFormatIndicator(MqttPayloadFormatIndicator willPayloadFormatIndicator)
        {
            _options.WillPayloadFormatIndicator = willPayloadFormatIndicator;
            return this;
        }

        public MqttClientOptionsBuilder WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel willQualityOfServiceLevel)
        {
            _options.WillQualityOfServiceLevel = willQualityOfServiceLevel;
            return this;
        }

        public MqttClientOptionsBuilder WithWillResponseTopic(string willResponseTopic)
        {
            _options.WillResponseTopic = willResponseTopic;
            return this;
        }

        public MqttClientOptionsBuilder WithWillRetain(bool willRetain = true)
        {
            _options.WillRetain = willRetain;
            return this;
        }

        public MqttClientOptionsBuilder WithWillMessageExpiryInterval(uint willMessageExpiryInterval)
        {
            _options.WillMessageExpiryInterval = willMessageExpiryInterval;
            return this;
        }

        public MqttClientOptionsBuilder WithWillTopic(string willTopic)
        {
            _options.WillTopic = willTopic;
            return this;
        }

        public MqttClientOptionsBuilder WithWillUserProperty(string name, string value)
        {
            if (_options.WillUserProperties == null)
            {
                _options.WillUserProperties = new List<MqttUserProperty>();
            }

            _options.WillUserProperties.Add(new MqttUserProperty(name, value));
            return this;
        }
    }
}