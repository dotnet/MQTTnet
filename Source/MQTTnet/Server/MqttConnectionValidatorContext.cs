using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttConnectionValidatorContext
    {
        public MqttConnectionValidatorContext(string clientId, string username, string password, MqttApplicationMessage willMessage, string endpoint)
        {
            ClientId = clientId;
            Username = username;
            Password = password;
            WillMessage = willMessage;
            Endpoint = endpoint;
        }

        public string ClientId { get; }

        public string Username { get; }

        public string Password { get; }

        public MqttApplicationMessage WillMessage { get; }

        public string Endpoint { get; }

        public MqttConnectReturnCode ReturnCode { get; set; } = MqttConnectReturnCode.ConnectionAccepted;
    }
}
