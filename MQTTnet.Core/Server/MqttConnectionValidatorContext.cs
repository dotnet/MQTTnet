using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public class MqttConnectionValidatorContext
    {
        public MqttConnectionValidatorContext(string clientId, string username, string password, MqttApplicationMessage willMessage)
        {
            ClientId = clientId;
            Username = username;
            Password = password;
            WillMessage = willMessage;
        }

        public string ClientId { get; }

        public string Username { get; }

        public string Password { get; }

        public MqttApplicationMessage WillMessage { get; }

        public MqttConnectReturnCode ReturnCode { get; set; } = MqttConnectReturnCode.ConnectionAccepted;
    }
}
