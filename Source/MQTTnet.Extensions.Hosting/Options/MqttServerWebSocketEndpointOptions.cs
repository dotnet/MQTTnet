using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Server
{
    public class MqttServerWebSocketEndpointOptions  : MqttServerWebSocketEndpointBaseOptions
    {

        public MqttServerWebSocketEndpointOptions()
        {
            Port = 80;
        }

    }
}
