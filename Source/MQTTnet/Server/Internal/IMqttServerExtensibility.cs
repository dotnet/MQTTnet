using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Server
{
    public interface IMqttServerExtensibility
    {

        MqttClientSessionsManager MqttClientSessionsManager { get; }

        IDictionary SessionItems { get; }

    }
}
