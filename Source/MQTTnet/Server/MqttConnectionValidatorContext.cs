using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttConnectionValidatorContext
    {
        public MqttConnectionValidatorContext(string clientId, string username, byte[] password, MqttApplicationMessage willMessage, string endpoint, bool isSecureConnection)
        {
            ClientId = clientId;
            Username = username;
            Password = password;
            WillMessage = willMessage;
            Endpoint = endpoint;
            IsSecureConnection = isSecureConnection;
        }

        public string ClientId { get; }

        public string Username { get; }

        public byte[] Password { get; }

        public MqttApplicationMessage WillMessage { get; }

        public string Endpoint { get; }

        public bool IsSecureConnection { get; }

        public MqttConnectReturnCode ReturnCode { get; set; } = MqttConnectReturnCode.ConnectionAccepted;
    }
}
