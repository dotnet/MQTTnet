// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System.Buffers;
using System.Text.Json;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Samples.Server;

public static class Server_Retained_Messages_Samples
{
    public static async Task Persist_Retained_Messages()
    {
        /*
         * This sample starts a simple MQTT server which will store all retained messages in a file.
         */

        var storePath = Path.Combine(Path.GetTempPath(), "RetainedMessages.json");

        var mqttServerFactory = new MqttServerFactory();

        // Due to security reasons the "default" endpoint (which is unencrypted) is not enabled by default!
        var mqttServerOptions = mqttServerFactory.CreateServerOptionsBuilder().WithDefaultEndpoint().Build();

        using (var server = mqttServerFactory.CreateMqttServer(mqttServerOptions))
        {
            // Make sure that the server will load the retained messages.
            server.LoadingRetainedMessageAsync += async eventArgs =>
            {
                try
                {
                    var models = await JsonSerializer.DeserializeAsync<List<MqttRetainedMessageModel>>(File.OpenRead(storePath)) ?? new List<MqttRetainedMessageModel>();
                    var retainedMessages = models.Select(m => m.ToApplicationMessage()).ToList();

                    eventArgs.LoadedRetainedMessages = retainedMessages;
                    Console.WriteLine("Retained messages loaded.");
                }
                catch (FileNotFoundException)
                {
                    // Ignore because nothing is stored yet.
                    Console.WriteLine("No retained messages stored yet.");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            };

            // Make sure to persist the changed retained messages.
            server.RetainedMessageChangedAsync += async eventArgs =>
            {
                try
                {
                    // This sample uses the property _StoredRetainedMessages_ which will contain all(!) retained messages.
                    // The event args also contain the affected retained message (property ChangedRetainedMessage). This can be
                    // used to write all retained messages to dedicated files etc. Then all files must be loaded and a full list
                    // of retained messages must be provided in the loaded event.

                    var models = eventArgs.StoredRetainedMessages.Select(MqttRetainedMessageModel.Create);

                    var buffer = JsonSerializer.SerializeToUtf8Bytes(models);
                    await File.WriteAllBytesAsync(storePath, buffer);
                    Console.WriteLine("Retained messages saved.");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            };

            // Make sure to clear the retained messages when they are all deleted via API.
            server.RetainedMessagesClearedAsync += _ =>
            {
                File.Delete(storePath);
                return Task.CompletedTask;
            };

            await server.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }

    sealed class MqttRetainedMessageModel
    {
        public string? ContentType { get; set; }
        public ReadOnlyMemory<byte> CorrelationData { get; set; }
        public ReadOnlySequence<byte> Payload { get; set; }
        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
        public string? ResponseTopic { get; set; }
        public string? Topic { get; set; }
        public List<MqttUserProperty>? UserProperties { get; set; }

        public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return new MqttRetainedMessageModel
            {
                Topic = message.Topic,

                // Create a copy of the buffer from the payload segment because
                // it cannot be serialized and deserialized with the JSON serializer.
                Payload = message.Payload,
                UserProperties = message.UserProperties,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                ContentType = message.ContentType,
                PayloadFormatIndicator = message.PayloadFormatIndicator,
                QualityOfServiceLevel = message.QualityOfServiceLevel

                // Other properties like "Retain" are not if interest in the storage.
                // That's why a custom model makes sense.
            };
        }

        public MqttApplicationMessage ToApplicationMessage()
        {
            return new MqttApplicationMessage
            {
                Topic = Topic,
                Payload = Payload,
                PayloadFormatIndicator = PayloadFormatIndicator,
                ResponseTopic = ResponseTopic,
                CorrelationData = CorrelationData,
                ContentType = ContentType,
                UserProperties = UserProperties,
                QualityOfServiceLevel = QualityOfServiceLevel,
                Dup = false,
                Retain = true
            };
        }
    }
}