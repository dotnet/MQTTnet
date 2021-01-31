﻿using MQTTnet.Client.Receiving;

namespace MQTTnet
{
    public interface IApplicationMessageReceiver
    {
        /// <summary>
        /// Gets or sets the application message received handler that is fired every time a new message is received on the client's subscriptions.
        /// Hint: Initialize handlers before you connect the client to avoid issues.
        /// </summary>
        IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }
    }
}
