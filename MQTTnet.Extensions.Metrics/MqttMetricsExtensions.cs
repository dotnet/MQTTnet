using System;
using System.Diagnostics.Metrics;
using MQTTnet.Server;
using MQTTnet.Diagnostics.Instrumentation;

namespace MQTTnet.Extensions.Metrics
{
    public static class MqttMetricsExtensions
    {
        public static void AddMetrics(this IMqttServerMetricSource server, Meter meter)
        {
            ArgumentNullException.ThrowIfNull(meter, nameof(meter));

            meter.CreateObservableUpDownCounter("mqtt.sessions.count", server.GetActiveSessionCount);
            meter.CreateObservableCounter("mqtt.clients.count", server.GetActiveClientCount);
            
            var publishedMessageCounter = meter.CreateCounter<long>("mqtt.messages.published.count");
            server.InterceptingPublishAsync += (InterceptingPublishEventArgs arg) =>
            {
                publishedMessageCounter.Add(1);
                return Task.CompletedTask;
            };

            var successfulConnectionsCounter = meter.CreateCounter<long>(
                    name: "mqtt.clients.connected.count",
                    unit: "total",
                    description: "Cumulative total number of connections successfully established with MQTTnet");
            server.ClientConnectedAsync += (ClientConnectedEventArgs arg) =>
            {
                successfulConnectionsCounter.Add(1);
                return Task.CompletedTask;
            };
            
        }
    }
}
