// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet;

public sealed class MqttClientDefaultCertificateValidationHandler
{
    public static bool Handle(MqttClientCertificateValidationEventArgs eventArgs)
    {
        if (eventArgs.SslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (eventArgs.Chain.ChainStatus.Any(
                c => c.Status is X509ChainStatusFlags.RevocationStatusUnknown or X509ChainStatusFlags.Revoked or X509ChainStatusFlags.OfflineRevocation))
        {
            if (eventArgs.ClientOptions?.TlsOptions?.IgnoreCertificateRevocationErrors != true)
            {
                return false;
            }
        }

        if (eventArgs.Chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.PartialChain))
        {
            if (eventArgs.ClientOptions?.TlsOptions?.IgnoreCertificateChainErrors != true)
            {
                return false;
            }
        }

        return eventArgs.ClientOptions?.TlsOptions?.AllowUntrustedCertificates == true;
    }
}