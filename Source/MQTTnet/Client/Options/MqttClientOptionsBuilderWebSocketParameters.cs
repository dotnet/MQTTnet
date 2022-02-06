// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net;

namespace MQTTnet.Client
{
    public class MqttClientOptionsBuilderWebSocketParameters
    {
        public IDictionary<string, string> RequestHeaders { get; set; }

        public CookieContainer CookieContainer { get; set; }
    }
}
