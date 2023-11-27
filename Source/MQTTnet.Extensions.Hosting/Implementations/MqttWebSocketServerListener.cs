using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Extensions.Hosting.Options;
using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting.Implementations
{
    public sealed class MqttWebSocketServerListener : IDisposable
    {
        readonly MqttServerWebSocketConnectionHandler _connectionHandler;
        readonly MqttServerWebSocketEndpointBaseOptions _endpointOptions;
        readonly MqttServerOptions _serverOptions;

        HttpListener? _listener;

        public MqttWebSocketServerListener(
            MqttServerOptions serverOptions,
            MqttServerWebSocketEndpointBaseOptions endpointOptions,
            MqttServerWebSocketConnectionHandler connectionHandler)
        {
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _endpointOptions = endpointOptions ?? throw new ArgumentNullException(nameof(endpointOptions));
            _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
        }

        public void Dispose()
        {
            _listener?.Stop();
            _listener?.Close();
        }

        public bool Start(CancellationToken cancellationToken)
        {
            try
            {
                _listener = new HttpListener();

                if (_endpointOptions is MqttServerTlsWebSocketEndpointOptions tlsEndpointOptions)
                {
                    if (tlsEndpointOptions.BoundInterNetworkAddress != null && tlsEndpointOptions.BoundInterNetworkAddress != IPAddress.Any)
                    {
                        _listener.Prefixes.Add($"https://{tlsEndpointOptions.BoundInterNetworkAddress}:{tlsEndpointOptions.Port}/");
                    }

                    if (tlsEndpointOptions.BoundInterNetworkV6Address != null && tlsEndpointOptions.BoundInterNetworkV6Address != IPAddress.IPv6Any)
                    {
                        _listener.Prefixes.Add($"https://{tlsEndpointOptions.BoundInterNetworkV6Address}:{tlsEndpointOptions.Port}/");
                    }

                    if ((tlsEndpointOptions.BoundInterNetworkAddress == null || tlsEndpointOptions.BoundInterNetworkAddress == IPAddress.Any) &&
                        (tlsEndpointOptions.BoundInterNetworkV6Address == null || tlsEndpointOptions.BoundInterNetworkV6Address == IPAddress.IPv6Any))
                    {
                        _listener.Prefixes.Add($"https://*:{tlsEndpointOptions.Port}/");
                    }
                }
                else if (_endpointOptions is MqttServerWebSocketEndpointOptions defaultEndpointOptions)
                {
                    if (defaultEndpointOptions.BoundInterNetworkAddress != null && defaultEndpointOptions.BoundInterNetworkAddress != IPAddress.Any)
                    {
                        _listener.Prefixes.Add($"http://{defaultEndpointOptions.BoundInterNetworkAddress}:{defaultEndpointOptions.Port}/");
                    }

                    if (defaultEndpointOptions.BoundInterNetworkV6Address != null && defaultEndpointOptions.BoundInterNetworkV6Address != IPAddress.IPv6Any)
                    {
                        _listener.Prefixes.Add($"http://{defaultEndpointOptions.BoundInterNetworkV6Address}:{defaultEndpointOptions.Port}/");
                    }

                    if ((defaultEndpointOptions.BoundInterNetworkAddress == null || defaultEndpointOptions.BoundInterNetworkAddress == IPAddress.Any) &&
                        (defaultEndpointOptions.BoundInterNetworkV6Address == null || defaultEndpointOptions.BoundInterNetworkV6Address == IPAddress.IPv6Any))
                    {
                        _listener.Prefixes.Add($"http://127.0.0.1:{defaultEndpointOptions.Port}/"); // TODO: Correct this to proper wildcard
                    }
                }

                _listener.Start();

                Task.Run(() => AcceptClientConnectionsAsync(cancellationToken), cancellationToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        async Task AcceptClientConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = await _listener!.GetContextAsync();
                if (_serverOptions.TlsEndpointOptions.ClientCertificateRequired)
                {
                    var clientCertificate = await context.Request.GetClientCertificateAsync().ConfigureAwait(false);
                    using var chain = X509Chain.Create();
                    if (!_serverOptions.TlsEndpointOptions.RemoteCertificateValidationCallback(this, clientCertificate, chain, SslPolicyErrors.None))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.Close();

                        continue;
                    }
                }

                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();

                    continue;
                }

                var webSocketContext = await context.AcceptWebSocketAsync("MQTT", _serverOptions.WriterBufferSize, _serverOptions.KeepAliveMonitorInterval);

                _connectionHandler.HandleWebSocketConnection(webSocketContext, context);
            }
        }
    }
}