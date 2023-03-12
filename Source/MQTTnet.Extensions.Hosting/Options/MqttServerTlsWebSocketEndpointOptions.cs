using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Server
{
    public class MqttServerTlsWebSocketEndpointOptions : MqttServerWebSocketEndpointBaseOptions
    {

        public MqttServerTlsWebSocketEndpointOptions()
        {
            Port = 443;
        }

    }
}
