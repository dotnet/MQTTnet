// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;
using MQTTnet.Protocol;

namespace MQTTnet.PowerShell;

public sealed class MqttSession : IDisposable
{
    public MqttSession()
    {
        var factory = new MqttClientFactory();
        Client = factory.CreateMqttClient();

        Client.ApplicationMessageReceivedAsync += e =>
        {
            MessageReceived?.Invoke(this, new MqttMessage
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

        // Client.ConnectedAsync += e =>
        // {
        //     //Connected?.Invoke(this, e);
        //     return Task.CompletedTask;
        // };
        //
        // Client.DisconnectedAsync += e =>
        // {
        //     //Disconnected?.Invoke(this, e);
        //     return Task.CompletedTask;
        // };
    }

    // public event EventHandler<MqttClientConnectedEventArgs> Connected;
    // public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
    public event EventHandler<MqttMessage>? MessageReceived;

    public IMqttClient Client { get; }

    public void Dispose()
    {
        try
        {
            if (Client != null)
            {
                if (Client.IsConnected)
                {
                    Client.DisconnectAsync().Wait();
                }

                Client.Dispose();
            }
        }
        catch
        {
        }
    }
}