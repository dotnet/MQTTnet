using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using MQTTnet.Server;
using MQTTnet.Diagnostics.Instrumentation;

namespace MQTTnet.Extensions.Metrics
{
    public static class MqttMetricsExtensions
    {
        public static void AddMetrics(this IMqttServerMetricSource server, Meter meter)
        {
            if (meter == null)
            {
                throw new ArgumentNullException(nameof(meter));
            }

            meter.CreateObservableUpDownCounter("mqtt.sessions.count", server.GetActiveSessionCount);
            meter.CreateObservableCounter("mqtt.clients.count", server.GetActiveClientCount);
            
            var publishedMessageCounter = meter.CreateCounter<long>("mqtt.messages.published.count");
            server.InterceptingPublishAsync += (InterceptingPublishEventArgs arg) =>
            {
                publishedMessageCounter.Add(1);
                return Task.CompletedTask;
            };

            var deliveredMessageCounter = meter.CreateCounter<long>(
                    name: "mqtt.messages.delivered.count",
                    unit: "total",
                    description: "Cumulative total number of MQTTnet messages delivered to subscribers");

            var droppedQueueFullMessageCounter = meter.CreateCounter<long>(
                    name: "mqtt.messages.dropped.queuefull.count",
                    unit: "total",
                    description: "Cumulative total number of MQTTnet messages dropped because the queue is full");

            server.ApplicationMessageEnqueuedOrDroppedAsync += (ApplicationMessageEnqueuedEventArgs args) =>
            {
                if (args.IsDropped)
                {
                    droppedQueueFullMessageCounter.Add(1);
                }
                else
                {
                    deliveredMessageCounter.Add(1);
                }
                return Task.CompletedTask;
            };
            server.QueuedApplicationMessageOverwrittenAsync += (QueueMessageOverwrittenEventArgs args) =>
            {
                droppedQueueFullMessageCounter.Add(1);
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
