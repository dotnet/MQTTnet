// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet;

public sealed class MqttClientCertificateSelectionEventArgs : EventArgs
{
    public MqttClientCertificateSelectionEventArgs(
        string targetHost,
        X509CertificateCollection? localCertificates,
        X509Certificate? remoteCertificate,
        string[] acceptableIssuers,
        MqttClientTcpOptions tcpOptions)
    {
        TargetHost = targetHost;
        LocalCertificates = localCertificates;
        RemoteCertificate = remoteCertificate;
        AcceptableIssuers = acceptableIssuers;
        TcpOptions = tcpOptions ?? throw new ArgumentNullException(nameof(tcpOptions));
    }

    public string[] AcceptableIssuers { get; }

    public X509CertificateCollection? LocalCertificates { get; }

    [Obsolete("Typo! Use RemoteCertificate instead!")]
    public X509Certificate? RemoveCertificate => RemoteCertificate;

    public X509Certificate? RemoteCertificate { get; }

    public string TargetHost { get; }

    public MqttClientTcpOptions TcpOptions { get; }
}