using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.AspNetCore
{
    public interface IAspNetMqttServerOptions : IMqttServerOptions
    {
        Boolean ListenTcp { get; }
    }
}
