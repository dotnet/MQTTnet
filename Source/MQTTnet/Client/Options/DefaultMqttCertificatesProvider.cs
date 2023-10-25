// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client
{
    public sealed class DefaultMqttCertificatesProvider : IMqttClientCertificatesProvider
    {
        readonly X509Certificate2Collection _certificates;

        public DefaultMqttCertificatesProvider(X509Certificate2Collection certificates)
        {
            _certificates = certificates;
        }

        public DefaultMqttCertificatesProvider(IEnumerable<X509Certificate> certificates)
        {
            if (certificates != null)
            {
                _certificates = new X509Certificate2Collection();
                foreach (var certificate in certificates)
                {
                    _certificates.Add(certificate);
                }
            }
        }

        public X509CertificateCollection GetCertificates()
        {
            return _certificates;
        }
    }
}