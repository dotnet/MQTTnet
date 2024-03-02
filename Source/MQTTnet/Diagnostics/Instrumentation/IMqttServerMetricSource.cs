using System;
using System.Threading.Tasks;
using MQTTnet.Server;

namespace MQTTnet.Diagnostics.Instrumentation
{
    public interface IMqttServerMetricSource
    {
        int GetActiveSessionCount();
        int GetActiveClientCount();
        event Func<ClientConnectedEventArgs, Task> ClientConnectedAsync;
        event Func<InterceptingPublishEventArgs, Task> InterceptingPublishAsync;
        event Func<ApplicationMessageEnqueuedEventArgs, Task> ApplicationMessageEnqueuedOrDroppedAsync;
        event Func<QueueMessageOverwrittenEventArgs, Task> QueuedApplicationMessageOverwrittenAsync;
    }
}