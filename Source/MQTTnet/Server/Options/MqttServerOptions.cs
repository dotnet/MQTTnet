using System;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server
{
    public class MqttServerOptions
    {
        public MqttServerTcpEndpointOptions DefaultEndpointOptions { get; } = new MqttServerTcpEndpointOptions();

        public MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsTcpEndpointOptions();
        
        public bool EnablePersistentSessions { get; set; }

        public int MaxPendingMessagesPerClient { get; set; } = 250;

        public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public TimeSpan KeepAliveMonitorInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        
        public IMqttServerStorage Storage { get; set; }

        public IMqttRetainedMessagesManager RetainedMessagesManager { get; set; } = new MqttRetainedMessagesManager();
    }
}
