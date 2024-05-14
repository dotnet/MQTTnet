// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_TLS_Samples
{
    public static async Task Run_Server_With_Self_Signed_Certificate()
    {
        /*
         * This sample starts a simple MQTT server which will accept any TCP connection.
         * It also has an encrypted connection using a self signed TLS certificate.
         *
         * See sample "Run_Minimal_Server" for more details.
         */

        var mqttServerFactory = new MqttServerFactory();

        // This certificate is self signed so that
        var certificate = CreateSelfSignedCertificate("1.3.6.1.5.5.7.3.1");

        var mqttServerOptions = new MqttServerOptionsBuilder().WithEncryptionCertificate(certificate).WithEncryptedEndpoint().Build();

        using (var mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions))
        {
            await mqttServer.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

            // Stop and dispose the MQTT server if it is no longer needed!
            await mqttServer.StopAsync();
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

            certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(oid) }, false));

            certRequest.CertificateExtensions.Add(sanBuilder.Build());

            using (var certificate = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddMinutes(-10), DateTimeOffset.Now.AddMinutes(10)))
            {
                var pfxCertificate = new X509Certificate2(
                    certificate.Export(X509ContentType.Pfx),
                    (string)null!,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                return pfxCertificate;
            }
        }
    }
}