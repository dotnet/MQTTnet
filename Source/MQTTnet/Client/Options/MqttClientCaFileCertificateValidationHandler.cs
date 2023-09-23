using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client.Options
{
    internal class MqttClientCaFileCertificateValidationHandler
    {
        internal static bool Handle(MqttClientCertificateValidationEventArgs cvArgs)
        {
            if (cvArgs.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }
            else
            {
                // only configure the chain if RemoteChainErrors is the only error 
                if (cvArgs.SslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    string caFile = cvArgs.ClientOptions.TlsOptions.CaFile;
                    if (!string.IsNullOrEmpty(caFile))
                    {
                        X509Certificate2Collection caCerts = new X509Certificate2Collection();
#if NET6_0_OR_GREATER
                        caCerts.ImportFromPemFile(caFile);
                        cvArgs.Chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        cvArgs.Chain.ChainPolicy.CustomTrustStore.AddRange(caCerts);
                        cvArgs.Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        X509Certificate2 cert = new X509Certificate2(cvArgs.Certificate);
                        var res = cvArgs.Chain.Build(cert);
                        if (!res)
                        {
                            cvArgs.Chain.ChainStatus.ToList().ForEach(s => Trace.TraceWarning(s.StatusInformation));
                        }
                        return res;
#else
                        return false;
#endif

                    }
                    else
                    {
#if NET6_0_OR_GREATER
                        Trace.TraceWarning($"CaFile '{caFile}' not found.");
#endif
                        return false;
                    }
                }
#if NET6_0_OR_GREATER
                Trace.TraceWarning(cvArgs.SslPolicyErrors.ToString());
#endif
                return false;
            }
        }
    }
}
