// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests.Mockups
{
    public class TestMqttCommunicationAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttChannelAdapter _adapter;

        public TestMqttCommunicationAdapterFactory(IMqttChannelAdapter adapter)
        {
            _adapter = adapter;
        }

        public IMqttChannelAdapter CreateClientAdapter(MqttClientOptions options, IMqttPacketInspectorHandler packetInspectorHandler, IMqttNetLogger logger)
        {
            return _adapter;
        }
    }
}
