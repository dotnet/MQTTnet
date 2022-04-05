// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Implementations;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestApplicationMessageReceivedHandler
    {
        readonly List<MqttApplicationMessageReceivedEventArgs> _receivedEventArgs = new List<MqttApplicationMessageReceivedEventArgs>();

        public TestApplicationMessageReceivedHandler(IMqttClient mqttClient)
        {
            mqttClient.ApplicationMessageReceivedAsync += MqttClientOnApplicationMessageReceivedAsync;
        }

        public List<MqttApplicationMessageReceivedEventArgs> ReceivedEventArgs
        {
            get
            {
                lock (_receivedEventArgs)
                {
                    return _receivedEventArgs.ToList();
                }
            }
        }

        public void AssertReceivedCountEquals(int expectedCount)
        {
            lock (_receivedEventArgs)
            {
                Assert.AreEqual(expectedCount, _receivedEventArgs.Count);
            }
        }

        Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            lock (_receivedEventArgs)
            {
                _receivedEventArgs.Add(eventArgs);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }
    }
}