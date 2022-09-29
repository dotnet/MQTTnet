// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ApplicationMessageProcessedEventArgs : EventArgs
    {
        public ApplicationMessageProcessedEventArgs(ManagedMqttApplicationMessage applicationMessage, Exception exception)
        {
            ApplicationMessage = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
            Exception = exception;
        }

        public ManagedMqttApplicationMessage ApplicationMessage { get; }

        /// <summary>
        ///     Gets or sets the exception which was thrown while sending the message.
        /// </summary>
        public Exception Exception { get; }

        public bool HasFailed => Exception != null;
    }
}