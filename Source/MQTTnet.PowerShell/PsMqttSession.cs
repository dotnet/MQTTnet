// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;

namespace MQTTnet.PowerShell;

public sealed class PsMqttSession : IDisposable
{
    readonly IMqttClient _client;

    public PsMqttSession()
    {
        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += e =>
        {
            MessageReceived?.Invoke(
                this,
                new PsMqttMessage
                {
                    Topic = e.ApplicationMessage.Topic,
                    Payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
                    RawPayload = e.ApplicationMessage.Payload.ToArray(),
                    UserProperties = e.ApplicationMessage.UserProperties,
                    QoS = (int)e.ApplicationMessage.QualityOfServiceLevel,
                    ContentType = e.ApplicationMessage.ContentType,
                    Retain = e.ApplicationMessage.Retain,
                    ResponseTopic = e.ApplicationMessage.ResponseTopic
                });

            return Task.CompletedTask;
        };
    }

    public event EventHandler<PsMqttMessage>? MessageReceived;

    public void Dispose()
    {
        _client.Dispose();
    }

    public IMqttClient GetClient()
    {
        return _client;
    }
}