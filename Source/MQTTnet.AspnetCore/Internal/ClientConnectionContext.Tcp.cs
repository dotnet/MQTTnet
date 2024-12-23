// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http.Features;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    partial class ClientConnectionContext
    {
        public static async Task<ClientConnectionContext> CreateAsync(MqttClientTcpOptions options, CancellationToken cancellationToken)
        {
            Socket socket;
            if (options.RemoteEndpoint is UnixDomainSocketEndPoint)
            {
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            }
            else if (options.AddressFamily == AddressFamily.Unspecified)
            {
                socket = new Socket(SocketType.Stream, options.ProtocolType);
            }
            else
            {
                socket = new Socket(options.AddressFamily, SocketType.Stream, options.ProtocolType);
            }

            if (options.LocalEndpoint != null)
            {
                socket.Bind(options.LocalEndpoint);
            }

            socket.ReceiveBufferSize = options.BufferSize;
            socket.SendBufferSize = options.BufferSize;

            if (options.ProtocolType == ProtocolType.Tcp && options.RemoteEndpoint is not UnixDomainSocketEndPoint)
            {
                // Other protocol types do not support the Nagle algorithm.
                socket.NoDelay = options.NoDelay;
            }

            if (options.LingerState != null)
            {
                socket.LingerState = options.LingerState;
            }

            if (options.DualMode.HasValue)
            {
                // It is important to avoid setting the flag if no specific value is set by the user
                // because on IPv4 only networks the setter will always throw an exception. Regardless
                // of the actual value.
                socket.DualMode = options.DualMode.Value;
            }

            try
            {
                await socket.ConnectAsync(options.RemoteEndpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }

            var networkStream = new NetworkStream(socket, ownsSocket: true);
            if (options.TlsOptions?.UseTls != true)
            {
                return new ClientConnectionContext(networkStream)
                {
                    LocalEndPoint = socket.LocalEndPoint,
                    RemoteEndPoint = socket.RemoteEndPoint,
                };
            }

            var targetHost = options.TlsOptions.TargetHost;
            if (string.IsNullOrEmpty(targetHost))
            {
                if (options.RemoteEndpoint is DnsEndPoint dns)
                {
                    targetHost = dns.Host;
                }
            }

            SslStream sslStream;
            if (options.TlsOptions.CertificateSelectionHandler != null)
            {
                sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: false,
                    InternalUserCertificateValidationCallback,
                    InternalUserCertificateSelectionCallback);
            }
            else
            {
                // Use a different constructor depending on the options for MQTTnet so that we do not have
                // to copy the exact same behavior of the selection handler.
                sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: false,
                    InternalUserCertificateValidationCallback);
            }

            var sslOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = options.TlsOptions.ApplicationProtocols,
                ClientCertificates = LoadCertificates(),
                EnabledSslProtocols = options.TlsOptions.SslProtocol,
                CertificateRevocationCheckMode = options.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : options.TlsOptions.RevocationMode,
                TargetHost = targetHost,
                CipherSuitesPolicy = options.TlsOptions.CipherSuitesPolicy,
                EncryptionPolicy = options.TlsOptions.EncryptionPolicy,
                AllowRenegotiation = options.TlsOptions.AllowRenegotiation
            };

            if (options.TlsOptions.TrustChain?.Count > 0)
            {
                sslOptions.CertificateChainPolicy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    VerificationFlags = X509VerificationFlags.IgnoreEndRevocationUnknown,
                    RevocationMode = options.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : options.TlsOptions.RevocationMode
                };

                sslOptions.CertificateChainPolicy.CustomTrustStore.AddRange(options.TlsOptions.TrustChain);
            }

            try
            {
                await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await sslStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            var connection = new ClientConnectionContext(sslStream)
            {
                LocalEndPoint = socket.LocalEndPoint,
                RemoteEndPoint = socket.RemoteEndPoint,
            };

            connection.Features.Set<ITlsConnectionFeature>(new TlsConnectionFeature(sslStream.LocalCertificate));
            return connection;


            X509Certificate InternalUserCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection? localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers)
            {
                var certificateSelectionHandler = options?.TlsOptions?.CertificateSelectionHandler;
                if (certificateSelectionHandler != null)
                {
                    var eventArgs = new MqttClientCertificateSelectionEventArgs(targetHost, localCertificates, remoteCertificate, acceptableIssuers, options);
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
                var certificateValidationHandler = options?.TlsOptions?.CertificateValidationHandler;
                if (certificateValidationHandler != null)
                {
                    var eventArgs = new MqttClientCertificateValidationEventArgs(x509Certificate, chain, sslPolicyErrors, options);
                    return certificateValidationHandler(eventArgs);
                }

                if (options?.TlsOptions?.IgnoreCertificateChainErrors ?? false)
                {
                    sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
                }

                return sslPolicyErrors == SslPolicyErrors.None;
            }

            X509CertificateCollection? LoadCertificates()
            {
                return options.TlsOptions.ClientCertificatesProvider?.GetCertificates();
            }
        }
    }
}
