// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace MQTTnet.AspNetCore;

public static class MqttSubProtocolSelector
{
    public static string SelectSubProtocol(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string subProtocol = null;
        if (request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
        {
            subProtocol = SelectSubProtocol(requestedSubProtocolValues);
        }

        return subProtocol;
    }

    public static string SelectSubProtocol(IList<string> requestedSubProtocolValues)
    {
        ArgumentNullException.ThrowIfNull(requestedSubProtocolValues);

        // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
        return requestedSubProtocolValues.OrderByDescending(p => p.Length).FirstOrDefault(p => p.ToLower().StartsWith("mqtt"));
    }
}