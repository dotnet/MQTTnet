using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Server.Internal
{
    public interface IMqttServerExtensibility
    {

        MqttClientSessionsManager MqttClientSessionsManager { get; }

    }
}
