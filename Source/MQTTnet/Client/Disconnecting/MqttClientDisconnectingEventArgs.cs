using System;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttClientDisconnectingEventArgs : EventArgs
    {
        public MqttClientDisconnectingEventArgs(MqttDisconnectReasonCode reason)
        {
            Reason = reason;
        }

        /// <summary>
        ///     Gets or sets the reason.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttDisconnectReasonCode Reason { get; }
    }
}
