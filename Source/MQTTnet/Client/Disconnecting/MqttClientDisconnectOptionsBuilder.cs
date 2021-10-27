namespace MQTTnet.Client.Disconnecting
{
    public sealed class MqttClientDisconnectOptionsBuilder
    {
        MqttClientDisconnectReason _reason = MqttClientDisconnectReason.NormalDisconnection;
        string _reasonString;
        
        public MqttClientDisconnectOptionsBuilder WithReasonString(string value)
        {
            _reasonString = value;
            return this;
        }
        
        public MqttClientDisconnectOptionsBuilder WithReason(MqttClientDisconnectReason value)
        {
            _reason = value;
            return this;
        }
        
        public MqttClientDisconnectOptions Build()
        {
            return new MqttClientDisconnectOptions
            {
                Reason = _reason,
                ReasonString = _reasonString
            };
        }
    }
}