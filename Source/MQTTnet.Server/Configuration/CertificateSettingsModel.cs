using System.IO;

namespace MQTTnet.Server.Configuration
{
    public class CertificateSettingsModel
    {
        /// <summary>
        /// Path to certificate.
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Password of certificate.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Read certificate file.
        /// </summary>
        public byte[] ReadCertificate()
        {
            if (string.IsNullOrEmpty(Path) || string.IsNullOrWhiteSpace(Path))
            {
                throw new FileNotFoundException("No path set");
            }

            if (!File.Exists(Path))
            {
                throw new FileNotFoundException($"Could not find Certificate in path: {Path}");
            }

            return File.ReadAllBytes(Path);
        }
    }
}
