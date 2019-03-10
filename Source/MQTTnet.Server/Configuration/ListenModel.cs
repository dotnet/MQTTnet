using System.IO;
using System.Net;

namespace MQTTnet.Server.Configuration
{
    /// <summary>
    /// Listen Entry Settings Model
    /// </summary>
    public class ListenModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ListenModel()
        {
        }

        /// <summary>
        /// Path to Certificate
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Enabled / Disable
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Listen Address
        /// </summary>
        public string IPv4 { get; set; }

        /// <summary>
        /// Listen Address
        /// </summary>
        public string IPv6 { get; set; }

        /// <summary>
        /// Listen Port
        /// </summary>
        public int Port { get; set; } = 1883;

        /// <summary>
        /// Read Certificate file
        /// </summary>
        /// <returns></returns>
        public byte[] ReadCertificate()
        {
            if (string.IsNullOrEmpty(CertificatePath) || string.IsNullOrWhiteSpace(CertificatePath))
            {
                throw new FileNotFoundException("No path set");
            }

            if (!File.Exists(CertificatePath))
            {
                throw new FileNotFoundException($"Could not find Certificate in path: {CertificatePath}");
            }

            return File.ReadAllBytes(CertificatePath);
        }

        /// <summary>
        /// Read IPv4
        /// </summary>
        /// <returns></returns>
        public IPAddress ReafIPv4()
        {
            if (IPv4 == "*")
            {
                return IPAddress.Parse("0.0.0.0");
            }

            if (IPv4 == "localhost")
            {
                return IPAddress.Parse("127.0.0.1");
            }

            if (IPAddress.TryParse(IPv4, out var ip))
            {
                return ip;
            }

            throw new System.Exception($"Could not parse IPv4 address: {IPv4}");
        }

        /// <summary>
        /// Read IPv4
        /// </summary>
        /// <returns></returns>
        public bool TryReadIPv4(out IPAddress address)
        {
            if (IPv4 == "*")
            {
                address = IPAddress.Parse("::");
                return true;
            }

            if (IPv4 == "localhost")
            {
                address = IPAddress.Parse("::1");
                return true;
            }

            if (IPv4 == "disable")
            {
                address = null;
                return false;
            }

            if (IPAddress.TryParse(IPv4, out var ip))
            {
                address = ip;
                return true;
            }
            else
            {
                throw new System.Exception($"Could not parse IPv4 address: {IPv4}");
            }
        }

        /// <summary>
        /// Read IPv6
        /// </summary>
        /// <returns></returns>
        public bool TryReadIPv6(out IPAddress address)
        {
            if (IPv6 == "*")
            {
                address = IPAddress.Parse("::");
                return true;
            }

            if (IPv6 == "localhost")
            {
                address = IPAddress.Parse("::1");
                return true;
            }

            if (IPv6 == "disable")
            {
                address = null;
                return false;
            }

            if (IPAddress.TryParse(IPv6, out var ip))
            {
                address = ip;
                return true;
            }
            else
            {
                throw new System.Exception($"Could not parse IPv6 address: {IPv6}");
            }
        }
    }
}