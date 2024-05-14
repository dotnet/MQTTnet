// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public sealed class MqttClientWebSocketOptionsBuilder
    {
        readonly MqttClientWebSocketOptions _webSocketOptions = new MqttClientWebSocketOptions();

        public MqttClientWebSocketOptions Build()
        {
            return _webSocketOptions;
        }

        public MqttClientWebSocketOptionsBuilder WithCookieContainer(CookieContainer cookieContainer)
        {
            _webSocketOptions.CookieContainer = cookieContainer;
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithCookieContainer(ICredentials credentials)
        {
            _webSocketOptions.Credentials = credentials;
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithProxyOptions(MqttClientWebSocketProxyOptions proxyOptions)
        {
            _webSocketOptions.ProxyOptions = proxyOptions;
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithProxyOptions(Action<MqttClientWebSocketProxyOptionsBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var proxyOptionsBuilder = new MqttClientWebSocketProxyOptionsBuilder();
            configure.Invoke(proxyOptionsBuilder);

            _webSocketOptions.ProxyOptions = proxyOptionsBuilder.Build();
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithRequestHeaders(IDictionary<string, string> requestHeaders)
        {
            _webSocketOptions.RequestHeaders = requestHeaders;
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithSubProtocols(ICollection<string> subProtocols)
        {
            _webSocketOptions.SubProtocols = subProtocols;
            return this;
        }

        public MqttClientWebSocketOptionsBuilder WithUri(string uri)
        {
            _webSocketOptions.Uri = uri;
            return this;
        }

#if !NETSTANDARD1_3
        public MqttClientWebSocketOptionsBuilder WithKeepAliveInterval(TimeSpan keepAliveInterval)
        {
            _webSocketOptions.KeepAliveInterval = keepAliveInterval;
            return this;
        }
#endif
#if !NETSTANDARD1_3
        public MqttClientWebSocketOptionsBuilder WithUseDefaultCredentials(bool useDefaultCredentials = true)
        {
            _webSocketOptions.UseDefaultCredentials = useDefaultCredentials;
            return this;
        }
#endif
    }
}