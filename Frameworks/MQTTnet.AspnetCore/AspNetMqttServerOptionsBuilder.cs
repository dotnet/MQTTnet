using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServerOptionsBuilder : MqttServerOptionsBuilder
    {


        public AspNetMqttServerOptionsBuilder():base(new AspNetMqttServerOptions())
        {
            
        }

        public AspNetMqttServerOptionsBuilder WithListenTcp(bool value)
        {
            ((AspNetMqttServerOptions)(_options)).ListenTcp = value;
            return this;
        }

        public new IAspNetMqttServerOptions Build()
        {
            return (AspNetMqttServerOptions)_options;
        }

    }

    

  
}
