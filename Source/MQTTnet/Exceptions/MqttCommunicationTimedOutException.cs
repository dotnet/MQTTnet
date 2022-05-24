// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Exceptions
{
    public sealed class MqttCommunicationTimedOutException : MqttCommunicationException
    {
        public MqttCommunicationTimedOutException() : base("The operation has timed out.")
        {
        }

        public MqttCommunicationTimedOutException(Exception innerException) : base("The operation has timed out.", innerException)
        {
        }
    }
}
