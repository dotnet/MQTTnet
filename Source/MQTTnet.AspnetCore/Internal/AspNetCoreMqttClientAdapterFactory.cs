// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        private readonly IConnectionFactory _connectionFactory;

        public AspNetCoreMqttClientAdapterFactory(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async ValueTask<IMqttChannelAdapter> CreateClientAdapterAsync(MqttClientOptions options, MqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions tcpOptions:
                    {
                        var endPoint = await CreateIPEndPointAsync(tcpOptions.RemoteEndpoint);
                        var connection = await _connectionFactory.ConnectAsync(endPoint);
                        await AuthenticateAsClientAsync(connection, tcpOptions);

                        var formatter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttBufferWriter(4096, 65535));
                        return new AspNetCoreMqttChannelAdapter(formatter, connection);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private static async ValueTask<IPEndPoint> CreateIPEndPointAsync(EndPoint endpoint)
        {
            if (endpoint is IPEndPoint ipEndPoint)
            {
                return ipEndPoint;
            }

            if (endpoint is DnsEndPoint dnsEndPoint)
            {
                var hostEntry = await Dns.GetHostEntryAsync(dnsEndPoint.Host);
                var address = hostEntry.AddressList.OrderBy(item => item.AddressFamily).FirstOrDefault();
                return address == null
                    ? throw new SocketException((int)SocketError.HostNotFound)
                    : new IPEndPoint(address, dnsEndPoint.Port);
            }

            throw new NotSupportedException("Only supports IPEndPoint or DnsEndPoint for now.");
        }


        private static async ValueTask AuthenticateAsClientAsync(ConnectionContext connection, MqttClientTcpOptions tcpOptions)
        {
            if (tcpOptions.TlsOptions?.UseTls != true)
            {
                return;
            }

            var targetHost = tcpOptions.TlsOptions.TargetHost;
            if (string.IsNullOrEmpty(targetHost))
            {
                if (tcpOptions.RemoteEndpoint is DnsEndPoint dns)
                {
                    targetHost = dns.Host;
                }
            }

            SslStream sslStream;
            var networkStream = new DuplexPipeStream(connection.Transport);
            if (tcpOptions.TlsOptions.CertificateSelectionHandler != null)
            {
                sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: true,
                    InternalUserCertificateValidationCallback,
                    InternalUserCertificateSelectionCallback);
            }
            else
            {
                // Use a different constructor depending on the options for MQTTnet so that we do not have
                // to copy the exact same behavior of the selection handler.
                sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: true,
                    InternalUserCertificateValidationCallback);
            }

            var sslOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = tcpOptions.TlsOptions.ApplicationProtocols,
                ClientCertificates = LoadCertificates(),
                EnabledSslProtocols = tcpOptions.TlsOptions.SslProtocol,
                CertificateRevocationCheckMode = tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : tcpOptions.TlsOptions.RevocationMode,
                TargetHost = targetHost,
                CipherSuitesPolicy = tcpOptions.TlsOptions.CipherSuitesPolicy,
                EncryptionPolicy = tcpOptions.TlsOptions.EncryptionPolicy,
                AllowRenegotiation = tcpOptions.TlsOptions.AllowRenegotiation
            };

            if (tcpOptions.TlsOptions.TrustChain?.Count > 0)
            {
                sslOptions.CertificateChainPolicy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    VerificationFlags = X509VerificationFlags.IgnoreEndRevocationUnknown,
                    RevocationMode = tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : tcpOptions.TlsOptions.RevocationMode
                };

                sslOptions.CertificateChainPolicy.CustomTrustStore.AddRange(tcpOptions.TlsOptions.TrustChain);
            }

            try
            {
                await sslStream.AuthenticateAsClientAsync(sslOptions).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await sslStream.DisposeAsync();
                throw;
            }

            connection.Transport = new StreamDuplexPipe(sslStream);
            connection.ConnectionClosed.Register(() =>
            {
                sslStream.Dispose();
            });
            connection.Features.Set<ITlsConnectionFeature>(new TlsConnectionFeature());

            X509Certificate InternalUserCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection? localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers)
            {
                var certificateSelectionHandler = tcpOptions?.TlsOptions?.CertificateSelectionHandler;
                if (certificateSelectionHandler != null)
                {
                    var eventArgs = new MqttClientCertificateSelectionEventArgs(targetHost, localCertificates, remoteCertificate, acceptableIssuers, tcpOptions);
                    return certificateSelectionHandler(eventArgs);
                }

                if (localCertificates?.Count > 0)
                {
                    return localCertificates[0];
                }

                return null!;
            }

            bool InternalUserCertificateValidationCallback(object sender, X509Certificate? x509Certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
            {
                var certificateValidationHandler = tcpOptions?.TlsOptions?.CertificateValidationHandler;
                if (certificateValidationHandler != null)
                {
                    var eventArgs = new MqttClientCertificateValidationEventArgs(x509Certificate, chain, sslPolicyErrors, tcpOptions);
                    return certificateValidationHandler(eventArgs);
                }

                if (tcpOptions?.TlsOptions?.IgnoreCertificateChainErrors ?? false)
                {
                    sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
                }

                return sslPolicyErrors == SslPolicyErrors.None;
            }

            X509CertificateCollection? LoadCertificates()
            {
                return tcpOptions.TlsOptions.ClientCertificatesProvider?.GetCertificates();
            }
        }

        private class StreamDuplexPipe(Stream stream) : IDuplexPipe
        {
            public PipeReader Input { get; } = PipeReader.Create(stream);

            public PipeWriter Output { get; } = PipeWriter.Create(stream);
        }

        private class TlsConnectionFeature : ITlsConnectionFeature
        {
            public X509Certificate2? ClientCertificate { get; set; }

            public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(ClientCertificate);
            }
        }
    }
}
