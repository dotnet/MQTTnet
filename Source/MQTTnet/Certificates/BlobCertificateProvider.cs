// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Certificates;

public class BlobCertificateProvider(byte[] blob) : ICertificateProvider
{
    public byte[] Blob { get; } = blob ?? throw new ArgumentNullException(nameof(blob));

    public string? Password { get; set; }

    public X509Certificate2 GetCertificate()
    {
        if (string.IsNullOrEmpty(Password))
        {
            // Use a different overload when no password is specified. Otherwise, the constructor will fail.
            return new X509Certificate2(Blob);
        }

        return new X509Certificate2(Blob, Password);
    }
}