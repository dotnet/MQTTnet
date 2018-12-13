using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MQTTnet.Client
{
    public class MqttClientOptionsBuilderWebSocketParameters
    {
        public IDictionary<string, string> RequestHeaders { get; set; }

        public CookieContainer CookierContainer { get; set; }
    }
}
