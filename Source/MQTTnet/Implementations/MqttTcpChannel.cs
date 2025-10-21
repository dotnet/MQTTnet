// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Channel;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Implementations;

public sealed class MqttTcpChannel : IMqttChannel
{
    readonly MqttClientOptions _clientOptions;
    readonly MqttClientTcpOptions _tcpOptions;

    Stream _stream;

    public MqttTcpChannel()
    {
    }

    public MqttTcpChannel(MqttClientOptions clientOptions) : this()
    {
        _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
        _tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;

        IsSecureConnection = clientOptions.ChannelOptions?.TlsOptions?.UseTls == true;
    }

    public MqttTcpChannel(Stream stream, EndPoint localEndPoint, EndPoint remoteEndPoint, X509Certificate2 clientCertificate) : this()
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;

        IsSecureConnection = stream is SslStream;
        ClientCertificate = clientCertificate;
    }

    public X509Certificate2 ClientCertificate { get; }

    public bool IsSecureConnection { get; }


    public EndPoint LocalEndPoint { get; private set; }

    public EndPoint RemoteEndPoint { get; private set; }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        CrossPlatformSocket socket = null;
        try
        {
            if (_tcpOptions.AddressFamily == AddressFamily.Unspecified)
            {
                socket = new CrossPlatformSocket(_tcpOptions.ProtocolType);
            }
            else
            {
                socket = new CrossPlatformSocket(_tcpOptions.AddressFamily, _tcpOptions.ProtocolType);
            }

            if (_tcpOptions.LocalEndpoint != null)
            {
                socket.Bind(_tcpOptions.LocalEndpoint);
            }

            socket.ReceiveBufferSize = _tcpOptions.BufferSize;
            socket.SendBufferSize = _tcpOptions.BufferSize;
            socket.SendTimeout = (int)_clientOptions.Timeout.TotalMilliseconds;

            if (_tcpOptions.ProtocolType == ProtocolType.Tcp)
            {
                // Other protocol types do not support the Nagle algorithm.
                socket.NoDelay = _tcpOptions.NoDelay;
            }

            if (socket.LingerState != null)
            {
                socket.LingerState = _tcpOptions.LingerState;
            }

            if (_tcpOptions.DualMode != null)
            {
                // It is important to avoid setting the flag if no specific value is set by the user
                // because on IPv4 only networks the setter will always throw an exception. Regardless
                // of the actual value.
                socket.DualMode = _tcpOptions.DualMode.Value;
            }

            await socket.ConnectAsync(_tcpOptions.RemoteEndpoint, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var networkStream = socket.GetStream();

            if (_tcpOptions.TlsOptions?.UseTls == true)
            {
                var targetHost = _tcpOptions.TlsOptions.TargetHost;
                if (string.IsNullOrEmpty(targetHost))
                {
                    if (_tcpOptions.RemoteEndpoint is DnsEndPoint dns)
                    {
                        targetHost = dns.Host;
                    }
                }

                SslStream sslStream;
                if (_tcpOptions.TlsOptions.CertificateSelectionHandler != null)
                {
                    sslStream = new SslStream(networkStream, false, InternalUserCertificateValidationCallback, InternalUserCertificateSelectionCallback);
                }
                else
                {
                    // Use a different constructor depending on the options for MQTTnet so that we do not have
                    // to copy the exact same behavior of the selection handler.
                    sslStream = new SslStream(networkStream, false, InternalUserCertificateValidationCallback);
                }

                try
                {
                    var sslOptions = CreateSslAuthenticationOptions();

                    sslOptions.TargetHost = targetHost;

                    if (_tcpOptions.TlsOptions.TrustChain?.Count > 0)
                    {
                        sslOptions.CertificateChainPolicy = new X509ChainPolicy
                        {
                            TrustMode = X509ChainTrustMode.CustomRootTrust,
                            VerificationFlags = X509VerificationFlags.IgnoreEndRevocationUnknown,
                            RevocationMode = _tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : _tcpOptions.TlsOptions.RevocationMode
                        };

                        sslOptions.CertificateChainPolicy.CustomTrustStore.AddRange(_tcpOptions.TlsOptions.TrustChain);
                    }

                    await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await sslStream.DisposeAsync().ConfigureAwait(false);

                    throw;
                }

                _stream = sslStream;
            }
            else
            {
                _stream = networkStream;
            }

            RemoteEndPoint = socket.RemoteEndPoint;
            LocalEndPoint = socket.LocalEndPoint;
        }
        catch
        {
            socket?.Dispose();
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return CompletedTask.Instance;
    }

    public void Dispose()
    {
        // When the stream is disposed it will also close the socket and this will also dispose it.
        // So there is no need to dispose the socket again.
        // https://stackoverflow.com/questions/3601521/should-i-manually-dispose-the-socket-after-closing-it
        try
        {
            _stream?.Close();
            _stream?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (NullReferenceException)
        {
        }
        finally
        {
            _stream = null;
        }
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var stream = _stream;

            if (stream == null)
            {
                return 0;
            }

            if (!stream.CanRead)
            {
                return 0;
            }

            return await stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Indicate a graceful socket close.
            return 0;
        }
        catch (IOException exception)
        {
            if (exception.InnerException is SocketException socketException)
            {
                ExceptionDispatchInfo.Capture(socketException).Throw();
            }

            throw;
        }
    }

    public async Task WriteAsync(ReadOnlySequence<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var stream = _stream;

            if (stream == null)
            {
                throw new MqttCommunicationException("The TCP connection is closed.");
            }

            foreach (var segment in buffer)
            {
                await stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException)
        {
            throw new MqttCommunicationException("The TCP connection is closed.");
        }
        catch (IOException exception)
        {
            if (exception.InnerException is SocketException socketException)
            {
                ExceptionDispatchInfo.Capture(socketException).Throw();
            }

            throw;
        }
    }

    SslClientAuthenticationOptions CreateSslAuthenticationOptions()
    {
        var sslOptions = new SslClientAuthenticationOptions
        {
            ApplicationProtocols = _tcpOptions.TlsOptions.ApplicationProtocols,
            ClientCertificates = LoadCertificates(),
            EnabledSslProtocols = _tcpOptions.TlsOptions.SslProtocol,
            CertificateRevocationCheckMode = _tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : _tcpOptions.TlsOptions.RevocationMode,
            CipherSuitesPolicy = _tcpOptions.TlsOptions.CipherSuitesPolicy,
            EncryptionPolicy = _tcpOptions.TlsOptions.EncryptionPolicy,
            AllowRenegotiation = _tcpOptions.TlsOptions.AllowRenegotiation
        };
        return sslOptions;
    }

    X509Certificate InternalUserCertificateSelectionCallback(
        object sender,
        string targetHost,
        X509CertificateCollection localCertificates,
        X509Certificate remoteCertificate,
        string[] acceptableIssuers)
    {
        var certificateSelectionHandler = _tcpOptions?.TlsOptions?.CertificateSelectionHandler;
        if (certificateSelectionHandler != null)
        {
            var eventArgs = new MqttClientCertificateSelectionEventArgs(targetHost, localCertificates, remoteCertificate, acceptableIssuers, _tcpOptions);
            return certificateSelectionHandler(eventArgs);
        }

        if (localCertificates?.Count > 0)
        {
            return localCertificates[0];
        }

        return null;
    }

    bool InternalUserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        var certificateValidationHandler = _tcpOptions?.TlsOptions?.CertificateValidationHandler;
        if (certificateValidationHandler != null)
        {
            var eventArgs = new MqttClientCertificateValidationEventArgs(x509Certificate, chain, sslPolicyErrors, _tcpOptions);
            return certificateValidationHandler(eventArgs);
        }

        if (_tcpOptions?.TlsOptions?.IgnoreCertificateChainErrors ?? false)
        {
            sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
        }

        return sslPolicyErrors == SslPolicyErrors.None;
    }

    X509CertificateCollection LoadCertificates()
    {
        return _tcpOptions.TlsOptions.ClientCertificatesProvider?.GetCertificates();
    }
}