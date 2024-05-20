// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client;

public sealed class MqttClientTlsOptionsBuilder
{
    readonly MqttClientTlsOptions _tlsOptions = new()
    {
        // If someone used this builder the change is very high that TLS
        // should be actually used.
        UseTls = true
    };

    public MqttClientTlsOptions Build()
    {
        return _tlsOptions;
    }

    public MqttClientTlsOptionsBuilder UseTls(bool useTls = true)
    {
        _tlsOptions.UseTls = useTls;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithAllowUntrustedCertificates(bool allowUntrustedCertificates = true)
    {
        _tlsOptions.AllowUntrustedCertificates = allowUntrustedCertificates;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithCertificateValidationHandler(Func<MqttClientCertificateValidationEventArgs, bool> certificateValidationHandler)
    {
        if (certificateValidationHandler == null)
        {
            throw new ArgumentNullException(nameof(certificateValidationHandler));
        }

        _tlsOptions.CertificateValidationHandler = certificateValidationHandler;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithCertificateSelectionHandler(Func<MqttClientCertificateSelectionEventArgs, X509Certificate> certificateSelectionHandler)
    {
        if (certificateSelectionHandler == null)
        {
            throw new ArgumentNullException(nameof(certificateSelectionHandler));
        }

        _tlsOptions.CertificateSelectionHandler = certificateSelectionHandler;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithClientCertificates(IEnumerable<X509Certificate2> certificates)
    {
        if (certificates == null)
        {
            throw new ArgumentNullException(nameof(certificates));
        }

        _tlsOptions.ClientCertificatesProvider = new DefaultMqttCertificatesProvider(certificates);
        return this;
    }

    public MqttClientTlsOptionsBuilder WithClientCertificates(X509Certificate2Collection certificates)
    {
        if (certificates == null)
        {
            throw new ArgumentNullException(nameof(certificates));
        }

        _tlsOptions.ClientCertificatesProvider = new DefaultMqttCertificatesProvider(certificates);
        return this;
    }

    public MqttClientTlsOptionsBuilder WithClientCertificatesProvider(IMqttClientCertificatesProvider clientCertificatesProvider)
    {
        _tlsOptions.ClientCertificatesProvider = clientCertificatesProvider;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithIgnoreCertificateChainErrors(bool ignoreCertificateChainErrors = true)
    {
        _tlsOptions.IgnoreCertificateChainErrors = ignoreCertificateChainErrors;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithIgnoreCertificateRevocationErrors(bool ignoreCertificateRevocationErrors = true)
    {
        _tlsOptions.IgnoreCertificateRevocationErrors = ignoreCertificateRevocationErrors;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithRevocationMode(X509RevocationMode revocationMode)
    {
        _tlsOptions.RevocationMode = revocationMode;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithSslProtocols(SslProtocols sslProtocols)
    {
        _tlsOptions.SslProtocol = sslProtocols;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithTargetHost(string targetHost)
    {
        _tlsOptions.TargetHost = targetHost;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithAllowRenegotiation(bool allowRenegotiation = true)
    {
        _tlsOptions.AllowRenegotiation = allowRenegotiation;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithApplicationProtocols(List<SslApplicationProtocol> applicationProtocols)
    {
        _tlsOptions.ApplicationProtocols = applicationProtocols;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithCipherSuitesPolicy(CipherSuitesPolicy cipherSuitePolicy)
    {
        _tlsOptions.CipherSuitesPolicy = cipherSuitePolicy;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithCipherSuitesPolicy(EncryptionPolicy encryptionPolicy)
    {
        _tlsOptions.EncryptionPolicy = encryptionPolicy;
        return this;
    }

    public MqttClientTlsOptionsBuilder WithTrustChain(X509Certificate2Collection chain)
    {
        _tlsOptions.TrustChain = chain;
        return this;
    }
}