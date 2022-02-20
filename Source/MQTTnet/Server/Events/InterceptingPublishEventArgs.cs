// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MQTTnet.Server
{
    public sealed class InterceptingPublishEventArgs : EventArgs
    { 
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems { get; internal set; }

        /// <summary>
        /// Gets the response which will be sent to the client via the PUBACK etc. packets.
        /// </summary>
        public PublishResponse Response { get; } = new PublishResponse();
        
        /// <summary>
        /// Gets or sets whether the publish should be processed internally.
        /// </summary>
        public bool ProcessPublish { get; set; } = true;
        
        public bool CloseConnection { get; set; }
        
        /// <summary>
        /// Gets the cancellation token which can indicate that the client connection gets down.
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}
