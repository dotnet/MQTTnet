// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc
{
    public sealed class TopicGenerationContext
    {
        public TopicGenerationContext(IMqttClient mqttClient, MqttRpcClientOptions options, string methodName, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            QualityOfServiceLevel = qualityOfServiceLevel;
            MqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string MethodName { get; }

        public IMqttClient MqttClient { get; }

        public MqttRpcClientOptions Options { get; }

        /// <summary>
        ///     Gets or sets the quality of service level.
        ///     The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message
        ///     that defines the guarantee of delivery for a specific message.
        ///     There are 3 QoS levels in MQTT:
        ///     - At most once  (0): Message gets delivered no time, once or multiple times.
        ///     - At least once (1): Message gets delivered at least once (one time or more often).
        ///     - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }
    }
}