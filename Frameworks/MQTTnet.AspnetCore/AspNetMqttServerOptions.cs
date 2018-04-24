using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServerOptions : MqttServerOptions, IAspNetMqttServerOptions
    {
        public Boolean ListenTcp { get; set; } = true;
    }
}
