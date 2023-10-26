// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics.Instrumentation;
using MQTTnet.Extensions.Metrics;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Tests.Extensions
{
    [TestClass]
    public sealed class Metrics_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task All_Metrics()
        {
            int sessionCount = 0;
            int clientCount = 0;
            long connectedCount = 0;
            long publishedCount = 0;

            using (var testEnvironment = CreateTestEnvironment())
            using (Meter meter = new Meter("TEST"))
            using (MeterListener meterListener = new MeterListener())
            {
                meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    switch (instrument.Name)
                    {
                        case "mqtt.sessions.count":
                        case "mqtt.clients.count":
                        case "mqtt.clients.connected.count":
                        case "mqtt.messages.published.count":
                            listener.EnableMeasurementEvents(instrument);
                            break;
                    }
                };

                var metricsSource = new TestMetricsSource();
                MeasurementCallback<int> intCallback =
                    (Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state) =>
                    {
                        switch (instrument.Name)
                        {
                            case "mqtt.sessions.count":
                                sessionCount = measurement;
                                break;
                            case "mqtt.clients.count":
                                clientCount = measurement;
                                break;
                        }
                    };
                meterListener.SetMeasurementEventCallback(intCallback);
                MeasurementCallback<long> longCallback =
                    (Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state) =>
                    {
                        switch (instrument.Name)
                        {
                            case "mqtt.clients.connected.count":
                                connectedCount += measurement;
                                break;
                            case "mqtt.messages.published.count":
                                publishedCount += measurement;
                                break;
                        }
                    };
                meterListener.SetMeasurementEventCallback(longCallback);
                meterListener.Start();
                metricsSource.AddMetrics(meter);
                meterListener.RecordObservableInstruments();
                Assert.AreEqual(1000, clientCount);
                Assert.AreEqual(1001, sessionCount);

                await metricsSource.TriggerClientConnection();
                Assert.AreEqual(1, connectedCount);

                await metricsSource.TriggerPublish();
                Assert.AreEqual(1, publishedCount);
            }
        }

        private class TestMetricsSource : IMqttServerMetricSource
        {
            public event Func<ClientConnectedEventArgs, Task> ClientConnectedAsync;
            public event Func<InterceptingPublishEventArgs, Task> InterceptingPublishAsync;

            public int GetActiveClientCount()
            {
                return 1000;
            }

            public int GetActiveSessionCount()
            {
                return 1001;
            }

            public Task TriggerClientConnection()
            {
                var args = new ClientConnectedEventArgs(
                    new MqttConnectPacket(),
                    MqttProtocolVersion.V500,
                    null,
                    new Dictionary<string, object>());
                return ClientConnectedAsync(args);
            }

            public Task TriggerPublish()
            {
                var args = new InterceptingPublishEventArgs(
                    new MqttApplicationMessage(),
                    CancellationToken.None,
                    "client",
                    new Dictionary<string, object>());
                return InterceptingPublishAsync(args);
            }
        }
    }
}
#endif