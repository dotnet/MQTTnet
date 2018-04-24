using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public class AspNetMqttServerOptionsBuilder : MqttServerOptionsBuilder
    {

        private AspNetMqttServerOptions _aspNetOptions => (AspNetMqttServerOptions)_options;

        public AspNetMqttServerOptionsBuilder():base(new AspNetMqttServerOptions())
        {
            
        }

        public AspNetMqttServerOptionsBuilder WithListenTcp(bool value)
        {
            _aspNetOptions.ListenTcp = value;
            return this;
        }

        public new IAspNetMqttServerOptions Build()
        {
            return _aspNetOptions;
        }

    }

    

  
}
