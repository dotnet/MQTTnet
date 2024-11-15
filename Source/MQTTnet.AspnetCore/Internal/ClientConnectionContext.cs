using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Internal
{
    sealed class ClientConnectionContext : ConnectionContext
    {
        private readonly Stream _stream;
        private readonly CancellationTokenSource _connectionCloseSource = new();

        public override IDuplexPipe Transport { get; set; }

        public override CancellationToken ConnectionClosed
        {
            get => _connectionCloseSource.Token;
            set => throw new InvalidOperationException();
        }

        public override string ConnectionId { get; set; } = Guid.NewGuid().ToString();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();

        public ClientConnectionContext(Stream stream)
        {
            _stream = stream;
            Transport = new StreamTransport(stream);
        }

        public override async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync();
            _connectionCloseSource.Cancel();
            _connectionCloseSource.Dispose();
        }

        public override void Abort()
        {
            _stream.Close();
            _connectionCloseSource.Cancel();
        }


        public static async Task<ConnectionContext> CreateAsync(MqttClientTcpOptions tcpOptions, CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(tcpOptions.RemoteEndpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }

            var networkStream = new NetworkStream(socket, ownsSocket: true);
            if (tcpOptions.TlsOptions?.UseTls != true)
            {
                return new ClientConnectionContext(networkStream);
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
            if (tcpOptions.TlsOptions.CertificateSelectionHandler != null)
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
                await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await sslStream.DisposeAsync();
                throw;
            }

            var connection = new ClientConnectionContext(sslStream)
            {
                LocalEndPoint = socket.LocalEndPoint,
                RemoteEndPoint = socket.RemoteEndPoint,
            };
            connection.Features.Set<ITlsConnectionFeature>(TlsConnectionFeature.Instance);
            connection.Features.Set<IConnectionSocketFeature>(new ConnectionSocketFeature(socket));
            return connection;


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


        private class StreamTransport(Stream stream) : IDuplexPipe
        {
            public PipeReader Input { get; } = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));

            public PipeWriter Output { get; } = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        private class TlsConnectionFeature : ITlsConnectionFeature
        {
            public static readonly TlsConnectionFeature Instance = new();

            public X509Certificate2? ClientCertificate { get; set; }

            public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(ClientCertificate);
            }
        }

        private class ConnectionSocketFeature(Socket socket) : IConnectionSocketFeature
        {
            public Socket Socket { get; } = socket;
        }
    }
}
