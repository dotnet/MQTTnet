using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttConnectionValidatorContext
    {
        public MqttConnectionValidatorContext(string clientId, X509Certificate clientCertificate, string username, string password, MqttApplicationMessage willMessage, string endpoint)
        {
            ClientId = clientId;
            ClientCertificate = clientCertificate;
            Username = username;
            Password = password;
            WillMessage = willMessage;
            Endpoint = endpoint;
        }

        public string ClientId { get; }

        public X509Certificate ClientCertificate { get; }

        public string Username { get; }

        public string Password { get; }

        public MqttApplicationMessage WillMessage { get; }

        public string Endpoint { get; }

        public MqttConnectReturnCode ReturnCode { get; set; } = MqttConnectReturnCode.ConnectionAccepted;
    }
}
