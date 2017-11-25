using System;

namespace MQTTnet.Server
{
    public interface IMqttServerOptions
    {
        Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; }
        int ConnectionBacklog { get; }
        Action<MqttConnectionValidatorContext> ConnectionValidator { get; }
        TimeSpan DefaultCommunicationTimeout { get; }
        MqttServerDefaultEndpointOptions DefaultEndpointOptions { get; }
        IMqttServerStorage Storage { get; }
        Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; }
        MqttServerTlsEndpointOptions TlsEndpointOptions { get; }
    }
}