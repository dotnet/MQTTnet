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
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    // missing certificate builder api means tests won't work for older frameworks
#if !(NET452 || NET461)
    [TestClass]
#endif
    public sealed class HotSwapCerts_Tests
    {
        readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);

        [TestMethod]
        public void ClientCertChangeWithoutServerUpdateFailsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                server.StartServer().Wait();

                client01.Connect();

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);

                client01.HotSwapClientCert();
                server.ForceDisconnectAsync(client01).Wait(DEFAULT_TIMEOUT);
                client01.WaitForDisconnectOrFail(DEFAULT_TIMEOUT);

                client01.WaitForConnectToFail(DEFAULT_TIMEOUT);
            }
        }

        [TestMethod]
        public void ClientCertChangeWithServerUpdateAcceptsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                server.StartServer().Wait();
                client01.Connect();

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);

                client01.HotSwapClientCert();
                server.ForceDisconnectAsync(client01).Wait(DEFAULT_TIMEOUT);
                client01.WaitForDisconnectOrFail(DEFAULT_TIMEOUT);

                server.InstallNewClientCert(client01.GetCurrentClientCert());

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);
            }
        }

        [TestMethod]
        public void ServerCertChangeWithClientCertUpdateAllowsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                server.StartServer().Wait();
                client01.Connect();

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);
                server.HotSwapServerCert();

                server.ForceDisconnectAsync(client01).Wait(DEFAULT_TIMEOUT);
                client01.WaitForDisconnectOrFail(DEFAULT_TIMEOUT);
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);
            }
        }

        [TestMethod]
        public void ServerCertChangeWithoutClientCertUpdateFailsReconnect()
        {
            using (var server = new ServerTestHarness())
            using (var client01 = new ClientTestHarness())
            {
                server.InstallNewClientCert(client01.GetCurrentClientCert());
                client01.InstallNewServerCert(server.GetCurrentServerCert());

                server.StartServer().Wait();
                client01.Connect();

                client01.WaitForConnectOrFail(DEFAULT_TIMEOUT);
                server.HotSwapServerCert();

                server.ForceDisconnectAsync(client01).Wait(DEFAULT_TIMEOUT);
                client01.WaitForDisconnectOrFail(DEFAULT_TIMEOUT);

                client01.WaitForConnectToFail(DEFAULT_TIMEOUT);
            }
        }

        static X509Certificate2 CreateSelfSignedCertificate(string oid)
        {
#if NET452 || NET461
                throw new NotImplementedException();
#else
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
#endif
        }

        class ClientTestHarness : IDisposable
        {
            IManagedMqttClient _client;
            readonly HotSwappableClientCertProvider _hotSwapClient = new HotSwappableClientCertProvider();

            public string ClientID => _client.InternalClient.Options.ClientId;

            public void ClearServerCerts()
            {
                _hotSwapClient.ClearServerCerts();
            }

            public void Connect()
            {
                Run_Client_Connection().Wait();
            }

            public void Dispose()
            {
                _client.Dispose();
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

            public void WaitForConnect(TimeSpan timeout)
            {
                var timer = Stopwatch.StartNew();
                while ((_client == null || !_client.IsConnected) && timer.Elapsed < timeout)
                {
                    Thread.Sleep(5);
                }
            }

            public void WaitForConnectOrFail(TimeSpan timeout)
            {
                Assert.IsFalse(_client.IsConnected, "Client should be disconnected before waiting for connect.");

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

            public void WaitForDisconnect(TimeSpan timeout)
            {
                var timer = Stopwatch.StartNew();
                while ((_client == null || _client.IsConnected) && timer.Elapsed < timeout)
                {
                    Thread.Sleep(5);
                }
            }

            public void WaitForDisconnectOrFail(TimeSpan timeout)
            {
                WaitForConnect(timeout);

                Assert.IsNotNull(_client, "Client was never initialized");
                Assert.IsFalse(_client.IsConnected, $"Client connection should have disconnected after {timeout}");
            }

            async Task Run_Client_Connection()
            {
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTlsOptions(
                        o => o.WithClientCertificatesProvider(_hotSwapClient)
                            .WithCertificateValidationHandler(_hotSwapClient.OnCertifciateValidation)
                            .WithSslProtocols(SslProtocols.Tls12))
                    .WithTcpServer("localhost")
                    .WithCleanSession()
                    .WithProtocolVersion(MqttProtocolVersion.V500);
                var mqttClientOptions = optionsBuilder.Build();

                var managedClientOptionsBuilder = new ManagedMqttClientOptionsBuilder().WithClientOptions(mqttClientOptions);
                var managedClientOptions = managedClientOptionsBuilder.Build();

                var factory = new MqttFactory();
                var mqttClient = factory.CreateManagedMqttClient();
                _client = mqttClient;

                await mqttClient.StartAsync(managedClientOptions);
            }
        }

        class ServerTestHarness : IDisposable
        {
            CancellationTokenSource _cts = new CancellationTokenSource();
            readonly HotSwappableServerCertProvider _hotSwapServer = new HotSwappableServerCertProvider();
            MqttServer _server;

            public void ClearClientCerts()
            {
                _hotSwapServer.ClearClientCerts();
            }

            public void Dispose()
            {
                if (_server != null)
                {
                    _server.StopAsync().Wait();
                    _server.Dispose();
                }

                if (_hotSwapServer != null)
                {
                    _hotSwapServer.Dispose();
                }
            }

            public async Task ForceDisconnectAsync(ClientTestHarness client)
            {
                await _server.DisconnectClientAsync(client.ClientID, MqttDisconnectReasonCode.UnspecifiedError);
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

            public async Task StartServer()
            {
                var mqttFactory = new MqttFactory();

                var mqttServerOptions = new MqttServerOptionsBuilder().WithEncryptionCertificate(_hotSwapServer)
                    .WithRemoteCertificateValidationCallback(_hotSwapServer.RemoteCertificateValidationCallback)
                    .WithEncryptedEndpoint()
                    .Build();
                mqttServerOptions.TlsEndpointOptions.ClientCertificateRequired = true;
                _server = mqttFactory.CreateMqttServer(mqttServerOptions);
                await _server.StartAsync();
            }
        }

        class HotSwappableClientCertProvider : IMqttClientCertificatesProvider
        {
            X509Certificate2Collection _certificates;
            ConcurrentBag<X509Certificate2> ServerCerts = new ConcurrentBag<X509Certificate2>();

            public HotSwappableClientCertProvider()
            {
                _certificates = new X509Certificate2Collection(CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.2"));
            }

            public void ClearServerCerts()
            {
                ServerCerts = new ConcurrentBag<X509Certificate2>();
            }

            public X509CertificateCollection GetCertificates()
            {
                return new X509Certificate2Collection(_certificates);
            }

            public void HotSwapCert()
            {
                var newCert = new X509Certificate2Collection(CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.2"));
                var oldCerts = Interlocked.Exchange(ref _certificates, newCert);
            }

            public void InstallNewServerCert(X509Certificate2 serverCert)
            {
                ServerCerts.Add(serverCert);
            }

            public bool OnCertifciateValidation(MqttClientCertificateValidationEventArgs certContext)
            {
                var serverCerts = ServerCerts.ToArray();

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

            void Dispose()
            {
                if (_certificates != null)
                {
                    foreach (var certs in _certificates)
                    {
#if !NET452
                        certs.Dispose();
#endif
                    }
                }
            }
        }

        class HotSwappableServerCertProvider : ICertificateProvider, IDisposable
        {
            X509Certificate2 _certificate;
            ConcurrentBag<X509Certificate2> ClientCerts = new ConcurrentBag<X509Certificate2>();

            public HotSwappableServerCertProvider()
            {
                _certificate = CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.1");
            }

            public void ClearClientCerts()
            {
                ClientCerts = new ConcurrentBag<X509Certificate2>();
            }

            public void Dispose()
            {
#if !NET452
                _certificate.Dispose();
#endif
            }

            public X509Certificate2 GetCertificate()
            {
                return _certificate;
            }

            public void HotSwapCert()
            {
                var newCert = CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.1");
                var oldCert = Interlocked.Exchange(ref _certificate, newCert);
#if !NET452
                oldCert.Dispose();
#endif
            }

            public void InstallNewClientCert(X509Certificate2 certificate)
            {
                ClientCerts.Add(certificate);
            }

            public bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                var serverCerts = ClientCerts.ToArray();

                var providedCert = certificate.GetRawCertData();
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
    }
}