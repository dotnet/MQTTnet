// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using MQTTnet.Certificates;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Adapter;

// ReSharper disable UnusedMember.Global
namespace MQTTnet.Server
{
    public class MqttServerOptionsBuilder
    {
        readonly MqttServerOptions _options = new MqttServerOptions();

        public MqttServerOptions Build()
        {
            return _options;
        }

        public MqttServerOptionsBuilder WithClientCertificate(RemoteCertificateValidationCallback validationCallback = null, bool checkCertificateRevocation = false)
        {
            _options.TlsEndpointOptions.ClientCertificateRequired = true;
            _options.TlsEndpointOptions.CheckCertificateRevocation = checkCertificateRevocation;
            _options.TlsEndpointOptions.RemoteCertificateValidationCallback = validationCallback;
            return this;
        }

        public MqttServerOptionsBuilder WithConnectionBacklog(int value)
        {
            _options.DefaultEndpointOptions.ConnectionBacklog = value;
            _options.TlsEndpointOptions.ConnectionBacklog = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultCommunicationTimeout(TimeSpan value)
        {
            _options.DefaultCommunicationTimeout = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpoint()
        {
            _options.DefaultEndpointOptions.IsEnabled = true;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpointBoundIPAddress(IPAddress value)
        {
            _options.DefaultEndpointOptions.BoundInterNetworkAddress = value ?? IPAddress.Any;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpointBoundIPV6Address(IPAddress value)
        {
            _options.DefaultEndpointOptions.BoundInterNetworkV6Address = value ?? IPAddress.Any;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpointPort(int value)
        {
            _options.DefaultEndpointOptions.Port = value;
            return this;
        }

        public MqttServerOptionsBuilder WithDefaultEndpointReuseAddress()
        {
            _options.DefaultEndpointOptions.ReuseAddress = true;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptedEndpoint()
        {
            _options.TlsEndpointOptions.IsEnabled = true;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptedEndpointBoundIPAddress(IPAddress value)
        {
            _options.TlsEndpointOptions.BoundInterNetworkAddress = value;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptedEndpointBoundIPV6Address(IPAddress value)
        {
            _options.TlsEndpointOptions.BoundInterNetworkV6Address = value;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptedEndpointPort(int value)
        {
            _options.TlsEndpointOptions.Port = value;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptionSslProtocol(SslProtocols value)
        {
            _options.TlsEndpointOptions.SslProtocol = value;
            return this;
        }

        public MqttServerOptionsBuilder WithKeepAlive()
        {
            _options.DefaultEndpointOptions.KeepAlive = true;
            _options.TlsEndpointOptions.KeepAlive = true;
            return this;
        }

        public MqttServerOptionsBuilder WithMaxPendingMessagesPerClient(int value)
        {
            _options.MaxPendingMessagesPerClient = value;
            return this;
        }

        public MqttServerOptionsBuilder WithoutDefaultEndpoint()
        {
            _options.DefaultEndpointOptions.IsEnabled = false;
            return this;
        }

        public MqttServerOptionsBuilder WithoutEncryptedEndpoint()
        {
            _options.TlsEndpointOptions.IsEnabled = false;
            return this;
        }

        /// <summary>
        ///     Usually the MQTT packets can be send partially to improve memory allocations.
        ///     This is done by using multiple TCP packets or WebSocket frames etc.
        ///     Unfortunately not all clients do support this and will close the connection when receiving partial packets.
        /// </summary>
        public MqttServerOptionsBuilder WithoutPacketFragmentation()
        {
            _options.DefaultEndpointOptions.AllowPacketFragmentation = false;
            _options.TlsEndpointOptions.AllowPacketFragmentation = false;
            return WithPacketFragmentationSelector(null);
        }

        public MqttServerOptionsBuilder WithPacketFragmentationSelector(Func<IMqttChannelAdapter, bool> selector)
        {
            _options.DefaultEndpointOptions.AllowPacketFragmentationSelector = selector;
            _options.TlsEndpointOptions.AllowPacketFragmentationSelector = selector;
            return this;
        }

        public MqttServerOptionsBuilder WithPersistentSessions(bool value = true)
        {
            _options.EnablePersistentSessions = value;
            return this;
        }

        public MqttServerOptionsBuilder WithRemoteCertificateValidationCallback(RemoteCertificateValidationCallback value)
        {
            _options.TlsEndpointOptions.RemoteCertificateValidationCallback = value;
            return this;
        }

        public MqttServerOptionsBuilder WithTcpKeepAliveInterval(int value)
        {
            _options.DefaultEndpointOptions.TcpKeepAliveInterval = value;
            _options.TlsEndpointOptions.TcpKeepAliveInterval = value;
            return this;
        }

        public MqttServerOptionsBuilder WithTcpKeepAliveRetryCount(int value)
        {
            _options.DefaultEndpointOptions.TcpKeepAliveRetryCount = value;
            _options.TlsEndpointOptions.TcpKeepAliveRetryCount = value;
            return this;
        }

        public MqttServerOptionsBuilder WithTcpKeepAliveTime(int value)
        {
            _options.DefaultEndpointOptions.TcpKeepAliveTime = value;
            _options.TlsEndpointOptions.TcpKeepAliveTime = value;
            return this;
        }

        public MqttServerOptionsBuilder WithTlsEndpointReuseAddress()
        {
            _options.TlsEndpointOptions.ReuseAddress = true;
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptionCertificate(byte[] value, IMqttServerCertificateCredentials credentials = null)
        {
            ArgumentNullException.ThrowIfNull(value);

            _options.TlsEndpointOptions.CertificateProvider = new BlobCertificateProvider(value)
            {
                Password = credentials?.Password
            };

            return this;
        }

        public MqttServerOptionsBuilder WithEncryptionCertificate(X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            _options.TlsEndpointOptions.CertificateProvider = new X509CertificateProvider(certificate);
            return this;
        }

        public MqttServerOptionsBuilder WithEncryptionCertificate(ICertificateProvider certificateProvider)
        {
            ArgumentNullException.ThrowIfNull(certificateProvider);

            _options.TlsEndpointOptions.CertificateProvider = certificateProvider;

            return this;
        }
    }
}