// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client.Internal
{
    public static class MqttClientResultFactory
    {
        public static readonly MqttClientPublishResultFactory PublishResult = new MqttClientPublishResultFactory();
        public static readonly MqttClientSubscribeResultFactory SubscribeResult = new MqttClientSubscribeResultFactory();
        public static readonly MqttClientUnsubscribeResultFactory UnsubscribeResult = new MqttClientUnsubscribeResultFactory();
    }
}