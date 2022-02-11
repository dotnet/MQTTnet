// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.Rpc
{
    public sealed class MqttRpcTopicPair
    {
        public string RequestTopic { get; set; }

        public string ResponseTopic { get; set; }
    }
}
