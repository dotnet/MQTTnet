using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Certificates;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    // missing certificate builder api means tests won't work for older frameworks

    [TestClass]
    public sealed class HotSwapCerts_Tests
    {
        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        [TestMethod]
        public async Task ClientCertChangeWithoutServerUpdateFailsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                await server.StartServer();

                await client01.Connect();

                client01.WaitForConnectOrFail(DefaultTimeout);

                client01.HotSwapClientCert();
                server.ForceDisconnectAsync(client01).Wait(DefaultTimeout);
                client01.WaitForDisconnectOrFail(DefaultTimeout);

                client01.WaitForConnectToFail(DefaultTimeout);
            }
        }

        [TestMethod]
        public async Task ClientCertChangeWithServerUpdateAcceptsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                await server.StartServer();

                await client01.Connect();

                client01.WaitForConnectOrFail(DefaultTimeout);

                client01.HotSwapClientCert();
                server.ForceDisconnectAsync(client01).Wait(DefaultTimeout);
                client01.WaitForDisconnectOrFail(DefaultTimeout);

                server.InstallNewClientCert(client01.GetCurrentClientCert());

                client01.WaitForConnectOrFail(DefaultTimeout);
            }
        }

        [TestMethod]
        public async Task ServerCertChangeWithClientCertUpdateAllowsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                await server.StartServer();
                await client01.Connect();

                client01.WaitForConnectOrFail(DefaultTimeout);
                server.HotSwapServerCert();

                server.ForceDisconnectAsync(client01).Wait(DefaultTimeout);
                client01.WaitForDisconnectOrFail(DefaultTimeout);
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                client01.WaitForConnectOrFail(DefaultTimeout);
            }
        }

        [TestMethod]
        public async Task ServerCertChangeWithoutClientCertUpdateFailsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                await server.StartServer();
                await client01.Connect();

                client01.WaitForConnectOrFail(DefaultTimeout);
                server.HotSwapServerCert();

                server.ForceDisconnectAsync(client01).Wait(DefaultTimeout);
                client01.WaitForDisconnectOrFail(DefaultTimeout);

                client01.WaitForConnectToFail(DefaultTimeout);
            }
        }

        static X509Certificate2 CreateSelfSignedCertificate(string oid)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");

            using (var rsa = RSA.Create())
            {
                var certRequest = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

                certRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(oid) }, false));

                certRequest.CertificateExtensions.Add(sanBuilder.Build());

                using (var certificate = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddMinutes(-10), DateTimeOffset.Now.AddMinutes(10)))
                {
                    var pfxCertificate = new X509Certificate2(
                        certificate.Export(X509ContentType.Pfx),
                        (string)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                    return pfxCertificate;
                }
            }
        }

        sealed class ClientTestHarness : IDisposable
        {
            readonly HotSwappableClientCertProvider _hotSwapClient = new HotSwappableClientCertProvider();

            IMqttClient _client;

            public string ClientId => _client.Options.ClientId;

            public Task Connect()
            {
                return Run_Client_Connection();
            }

            public void Dispose()
            {
                _client.Dispose();
                _hotSwapClient.Dispose();
            }

            public X509Certificate2 GetCurrentClientCert()
            {
                var result = _hotSwapClient.GetCertificates()[0];
                return new X509Certificate2(result);
            }

            public void HotSwapClientCert()
            {
                _hotSwapClient.HotSwapCert();
            }

            public void InstallNewServerCert(X509Certificate2 serverCert)
            {
                _hotSwapClient.InstallNewServerCert(serverCert);
            }

            public void WaitForConnectOrFail(TimeSpan timeout)
            {
                Thread.Sleep(100);

                if (!_client.IsConnected)
                {
                    _client.ReconnectAsync().Wait(timeout);
                }

                WaitForConnect(timeout);

                Assert.IsNotNull(_client, "Client was never initialized");
                Assert.IsTrue(_client.IsConnected, $"Client connection failed after {timeout}");
            }

            public void WaitForConnectToFail(TimeSpan timeout)
            {
                Assert.IsFalse(_client.IsConnected, "Client should be disconnected before waiting for connect.");

                WaitForConnect(timeout);

                Assert.IsNotNull(_client, "Client was never initialized");
                Assert.IsFalse(_client.IsConnected, "Client connection success but test wanted fail");
            }

            void WaitForDisconnect(TimeSpan timeout)
            {
                var timer = Stopwatch.StartNew();
                while ((_client == null || _client.IsConnected) && timer.Elapsed < timeout)
                {
                    Thread.Sleep(100);
                }
            }

            public void WaitForDisconnectOrFail(TimeSpan timeout)
            {
                WaitForDisconnect(timeout);

                Assert.IsNotNull(_client, "Client was never initialized");
                Assert.IsFalse(_client.IsConnected, $"Client connection should have disconnected after {timeout}");
            }

            async Task Run_Client_Connection()
            {
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTlsOptions(
                        o => o.WithClientCertificatesProvider(_hotSwapClient)
                            .WithCertificateValidationHandler(_hotSwapClient.OnCertificateValidation)
                            .WithSslProtocols(SslProtocols.Tls12))
                    .WithTcpServer("localhost")
                    .WithCleanSession()
                    .WithProtocolVersion(MqttProtocolVersion.V500);

                var mqttClientOptions = optionsBuilder.Build();

                var factory = new MqttClientFactory();
                var mqttClient = factory.CreateMqttClient();
                _client = mqttClient;

                await mqttClient.ConnectAsync(mqttClientOptions);
            }

            void WaitForConnect(TimeSpan timeout)
            {
                var timer = Stopwatch.StartNew();
                while ((_client == null || !_client.IsConnected) && timer.Elapsed < timeout)
                {
                    Thread.Sleep(100);
                }
            }
        }

        sealed class ServerTestHarness : IDisposable
        {
            readonly HotSwappableServerCertProvider _hotSwapServer = new HotSwappableServerCertProvider();

            MqttServer _server;

            public void Dispose()
            {
                if (_server != null)
                {
                    _server.StopAsync().Wait();
                    _server.Dispose();
                }

                _hotSwapServer?.Dispose();
            }

            public Task ForceDisconnectAsync(ClientTestHarness client)
            {
                return _server.DisconnectClientAsync(client.ClientId, MqttDisconnectReasonCode.UnspecifiedError);
            }

            public X509Certificate2 GetCurrentServerCert()
            {
                return _hotSwapServer.GetCertificate();
            }

            public void HotSwapServerCert()
            {
                _hotSwapServer.HotSwapCert();
            }

            public void InstallNewClientCert(X509Certificate2 serverCert)
            {
                _hotSwapServer.InstallNewClientCert(serverCert);
            }

            public Task StartServer()
            {
                var mqttServerFactory = new MqttServerFactory();

                var mqttServerOptions = new MqttServerOptionsBuilder().WithEncryptionCertificate(_hotSwapServer)
                    .WithRemoteCertificateValidationCallback(_hotSwapServer.RemoteCertificateValidationCallback)
                    .WithEncryptedEndpoint()
                    .Build();

                mqttServerOptions.TlsEndpointOptions.ClientCertificateRequired = true;

                _server = mqttServerFactory.CreateMqttServer(mqttServerOptions);
                return _server.StartAsync();
            }
        }

        class HotSwappableClientCertProvider : IMqttClientCertificatesProvider, IDisposable
        {
            X509Certificate2Collection _certificates;
            ConcurrentBag<X509Certificate2> _serverCerts = new ConcurrentBag<X509Certificate2>();

            public HotSwappableClientCertProvider()
            {
                _certificates = new X509Certificate2Collection(CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.2"));
            }

            public void Dispose()
            {
                if (_certificates != null)
                {
                    foreach (var certs in _certificates)
                    {
                        certs.Dispose();
                    }
                }
            }

            public X509CertificateCollection GetCertificates()
            {
                return new X509Certificate2Collection(_certificates);
            }

            public void HotSwapCert()
            {
                _certificates = new X509Certificate2Collection(CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.2"));
            }

            public void InstallNewServerCert(X509Certificate2 serverCert)
            {
                _serverCerts.Add(serverCert);
            }

            public bool OnCertificateValidation(MqttClientCertificateValidationEventArgs certContext)
            {
                var serverCerts = _serverCerts.ToArray();

                var providedCert = certContext.Certificate.GetRawCertData();
                for (int i = 0, n = serverCerts.Length; i < n; i++)
                {
                    var currentcert = serverCerts[i];

                    if (currentcert.RawData.SequenceEqual(providedCert))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        sealed class HotSwappableServerCertProvider : ICertificateProvider, IDisposable
        {
            readonly ConcurrentBag<X509Certificate2> _clientCerts = new ConcurrentBag<X509Certificate2>();
            X509Certificate2 _certificate;

            public HotSwappableServerCertProvider()
            {
                _certificate = CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.1");
            }

            public void Dispose()
            {
                _certificate.Dispose();
            }

            public X509Certificate2 GetCertificate()
            {
                return _certificate;
            }

            public void HotSwapCert()
            {
                var newCert = CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.1");
                var oldCert = Interlocked.Exchange(ref _certificate, newCert);

                oldCert.Dispose();
            }

            public void InstallNewClientCert(X509Certificate2 certificate)
            {
                _clientCerts.Add(certificate);
            }

            public bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                var serverCerts = _clientCerts.ToArray();

                var providedCert = certificate.GetRawCertData();
                for (int i = 0, n = serverCerts.Length; i < n; i++)
                {
                    var currentCert = serverCerts[i];

                    if (currentCert.RawData.SequenceEqual(providedCert))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
