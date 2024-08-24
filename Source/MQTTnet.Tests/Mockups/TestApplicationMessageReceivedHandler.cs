// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestApplicationMessageReceivedHandler
    {
        readonly List<MqttApplicationMessageReceivedEventArgs> _receivedEventArgs = new List<MqttApplicationMessageReceivedEventArgs>();

        public TestApplicationMessageReceivedHandler(IMqttClient mqttClient)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        }

        public int Count
        {
            get
            {
                lock (_receivedEventArgs)
                {
                    return _receivedEventArgs.Count;
                }
            }
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
            Assert.AreEqual(expectedCount, Count);
        }

        public string GeneratePayloadSequence()
        {
            var sequence = new StringBuilder();

            lock (_receivedEventArgs)
            {
                foreach (var receivedEventArg in _receivedEventArgs)
                {
                    var payload = receivedEventArg.ApplicationMessage.ConvertPayloadToString();

                    // An empty payload is not part of the sequence!
                    sequence.Append(payload);
                }
            }

            return sequence.ToString();
        }

        Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            lock (_receivedEventArgs)
            {
                _receivedEventArgs.Add(eventArgs);
            }

            return CompletedTask.Instance;
        }
    }
}