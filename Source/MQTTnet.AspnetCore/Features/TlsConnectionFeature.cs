// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed class TlsConnectionFeature : ITlsConnectionFeature
    {
        public static readonly TlsConnectionFeature WithoutClientCertificate = new(null);

        public X509Certificate2? ClientCertificate { get; set; }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ClientCertificate);
        }

        public TlsConnectionFeature(X509Certificate? clientCertificate)
        {
            ClientCertificate = clientCertificate as X509Certificate2;
        }
    }
}
