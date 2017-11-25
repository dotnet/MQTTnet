﻿using System;
using MQTTnet.Serializer;

namespace MQTTnet.Client
{
    public class MqttClientOptions : IMqttClientOptions
    {
        public MqttApplicationMessage WillMessage { get; set; }

        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

        public bool CleanSession { get; set; } = true;

        public IMqttClientCredentials Credentials { get; set; } = new MqttClientCredentials();

        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan CommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public IMqttClientChannelOptions ChannelOptions { get; set; }
    }
}
