// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ConnectingFailedEventArgs : EventArgs
    {
        public ConnectingFailedEventArgs(MqttClientConnectResult connectResult, Exception exception)
        {
            ConnectResult = connectResult;
            Exception = exception;
        }

        /// <summary>
        ///     This is null when the server was not reachable and thus no MQTT response was transmitted.
        /// </summary>
        public MqttClientConnectResult ConnectResult { get; }

        public Exception Exception { get; }
    }
}