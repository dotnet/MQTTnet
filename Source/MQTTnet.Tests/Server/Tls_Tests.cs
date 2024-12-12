using System;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Certificates;
using MQTTnet.Formatter;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Tls_Tests : BaseTestClass
    {
        static X509Certificate2 CreateCertificate(string oid)
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

        [TestMethod]
        public async Task Tls_Swap_Test()
        {
            using var testEnvironment = new TestEnvironment(TestContext, MqttProtocolVersion.V500);
            var serverOptionsBuilder = testEnvironment.ServerFactory.CreateServerOptionsBuilder();

            var firstOid = "1.3.6.1.5.5.7.3.1";
            var secondOid = "1.3.6.1.5.5.7.3.2";

            var certificateProvider = new CertificateProvider
            {
                CurrentCertificate = CreateCertificate(firstOid)
            };

            serverOptionsBuilder.WithoutDefaultEndpoint().WithEncryptedEndpoint().WithEncryptionSslProtocol(SslProtocols.Tls12).WithEncryptionCertificate(certificateProvider);

            var serverOptions = serverOptionsBuilder.Build();

            var server = testEnvironment.CreateServer(serverOptions);

            var publishedCount = 0;
            server.InterceptingPublishAsync += args =>
            {
                Interlocked.Increment(ref publishedCount);

                return Task.CompletedTask;
            };

            await server.StartAsync();

            var firstClient = await ConnectClientAsync(
                testEnvironment,
                args =>
                {
                    Assert.AreEqual(firstOid, ((X509Certificate2)args.Certificate).Extensions.OfType<X509EnhancedKeyUsageExtension>().First().EnhancedKeyUsages[0].Value);
                    return true;
                });

            var firstClientReceivedCount = 0;
            firstClient.ApplicationMessageReceivedAsync += args =>
            {
                Interlocked.Increment(ref firstClientReceivedCount);

                return Task.CompletedTask;
            };

            await firstClient.SubscribeAsync("TestTopic1");

            await firstClient.PublishAsync(
                new MqttApplicationMessage
                {
                    Topic = "TestTopic1",
                    PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                });

            await testEnvironment.Server.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(
                    new MqttApplicationMessage
                    {
                        Topic = "TestTopic1",
                        PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                    }));

            certificateProvider.CurrentCertificate = CreateCertificate(secondOid);

            // Validate that the certificate was switched
            var secondClient = await ConnectClientAsync(
                testEnvironment,
                args =>
                {
                    Assert.AreEqual(secondOid, ((X509Certificate2)args.Certificate).Extensions.OfType<X509EnhancedKeyUsageExtension>().First().EnhancedKeyUsages[0].Value);
                    return true;
                });

            var secondClientReceivedCount = 0;
            secondClient.ApplicationMessageReceivedAsync += args =>
            {
                Interlocked.Increment(ref secondClientReceivedCount);

                return Task.CompletedTask;
            };

            await secondClient.SubscribeAsync("TestTopic2");

            await firstClient.PublishAsync(
                new MqttApplicationMessage
                {
                    Topic = "TestTopic2",
                    PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                });

            await testEnvironment.Server.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(
                    new MqttApplicationMessage
                    {
                        Topic = "TestTopic2",
                        PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                    }));

            // Ensure first client still works
            await firstClient.PublishAsync(
                new MqttApplicationMessage
                {
                    Topic = "TestTopic1",
                    PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                });

            await testEnvironment.Server.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(
                    new MqttApplicationMessage
                    {
                        Topic = "TestTopic1",
                        PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 })
                    }));

            await Task.Delay(1000);

            Assert.AreEqual(6, publishedCount);
            Assert.AreEqual(4, firstClientReceivedCount);
            Assert.AreEqual(2, secondClientReceivedCount);

            await server.StopAsync();
        }

        static async Task<IMqttClient> ConnectClientAsync(TestEnvironment testEnvironment, Func<MqttClientCertificateValidationEventArgs, bool> certValidator)
        {
            var clientOptionsBuilder = testEnvironment.ClientFactory.CreateClientOptionsBuilder();
            clientOptionsBuilder.WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost", 8883)
                .WithTlsOptions(
                    o =>
                    {
                        o.WithSslProtocols(SslProtocols.Tls12).WithCertificateValidationHandler(certValidator);
                    });

            var clientOptions = clientOptionsBuilder.Build();
            return await testEnvironment.ConnectClient(clientOptions);
        }

        sealed class CertificateProvider : ICertificateProvider
        {
            public X509Certificate2 CurrentCertificate { get; set; }

            public X509Certificate2 GetCertificate()
            {
                return CurrentCertificate;
            }
        }
    }
}
