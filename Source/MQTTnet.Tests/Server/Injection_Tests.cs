using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Exceptions;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Injection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Enqueue_Application_Message_At_Session_Level()
        {
            using var testEnvironment = CreateTestEnvironment();

            var server = await testEnvironment.StartServer();
            var receiver1 = await testEnvironment.ConnectClient();
            var receiver2 = await testEnvironment.ConnectClient();
            var messageReceivedHandler1 = testEnvironment.CreateApplicationMessageHandler(receiver1);
            var messageReceivedHandler2 = testEnvironment.CreateApplicationMessageHandler(receiver2);

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];

            await receiver1.SubscribeAsync("#");
            await receiver2.SubscribeAsync("#");

            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTopic("InjectedOne").Build();

            var enqueued = clientStatus.TryEnqueueApplicationMessage(message, out var publishPacket);

            Assert.IsTrue(enqueued);

            await LongTestDelay();

            Assert.AreEqual(1, messageReceivedHandler1.ReceivedEventArgs.Count);
            Assert.AreEqual(publishPacket.PacketIdentifier, messageReceivedHandler1.ReceivedEventArgs[0].PacketIdentifier);
            Assert.AreEqual("InjectedOne", messageReceivedHandler1.ReceivedEventArgs[0].ApplicationMessage.Topic);

            // The second receiver should NOT receive the message.
            Assert.AreEqual(0, messageReceivedHandler2.ReceivedEventArgs.Count);
        }

        [TestMethod]
        public async Task Enqueue_Application_Message_At_Session_Level_QueueOverflow_DropNewMessageStrategy()
        {
            using var testEnvironment = CreateTestEnvironment(trackUnobservedTaskException: false);

            var server = await testEnvironment.StartServer(
                builder => builder
                    .WithMaxPendingMessagesPerClient(1)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage));

            var receiver = await testEnvironment.ConnectClient();

            var firstMessageOutboundPacketInterceptedTcs = new TaskCompletionSource();
            server.InterceptingOutboundPacketAsync += async args =>
            {
                // - The first message is dequeued normally and calls this delay
                // - The second message fills the outbound queue
                // - The third message overflows the outbound queue
                if (args.Packet is MqttPublishPacket)
                {
                    firstMessageOutboundPacketInterceptedTcs.SetResult();
                    await Task.Delay(TimeSpan.FromDays(1), args.CancellationToken);
                }
            };

            var firstMessageEvicted = false;
            var secondMessageEvicted = false;
            var thirdMessageEvicted = false;

            server.QueuedApplicationMessageOverwrittenAsync += args =>
            {
                if (args.Packet is not MqttPublishPacket publishPacket)
                {
                    return Task.CompletedTask;
                }

                switch (publishPacket.Topic)
                {
                    case "InjectedOne":
                        firstMessageEvicted = true;
                        break;
                    case "InjectedTwo":
                        secondMessageEvicted = true;
                        break;
                    case "InjectedThree":
                        thirdMessageEvicted = true;
                        break;
                }

                return Task.CompletedTask;
            };

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];
            await receiver.SubscribeAsync("#");

            var firstMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build(), out _);
            await firstMessageOutboundPacketInterceptedTcs.Task;

            var secondMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedTwo").Build(), out _);

            var thirdMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedThree").Build(), out _);

            // Due to the DropNewMessage strategy the third message will not be enqueued.
            // As a result, no existing messages in the queue will be dropped (evicted).
            Assert.IsTrue(firstMessageEnqueued);
            Assert.IsTrue(secondMessageEnqueued);
            Assert.IsFalse(thirdMessageEnqueued);

            Assert.IsFalse(firstMessageEvicted);
            Assert.IsFalse(secondMessageEvicted);
            Assert.IsFalse(thirdMessageEvicted);
        }


        [TestMethod]
        public async Task Enqueue_Application_Message_At_Session_Level_QueueOverflow_DropOldestQueuedMessageStrategy()
        {
            using var testEnvironment = CreateTestEnvironment(trackUnobservedTaskException: false);

            var server = await testEnvironment.StartServer(
                builder => builder
                    .WithMaxPendingMessagesPerClient(1)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage));

            var receiver = await testEnvironment.ConnectClient();

            var firstMessageOutboundPacketInterceptedTcs = new TaskCompletionSource();
            server.InterceptingOutboundPacketAsync += async args =>
            {
                // - The first message is dequeued normally and calls this delay
                // - The second message fills the outbound queue
                // - The third message overflows the outbound queue
                if (args.Packet is MqttPublishPacket)
                {
                    firstMessageOutboundPacketInterceptedTcs.SetResult();
                    await Task.Delay(TimeSpan.FromDays(1), args.CancellationToken);
                }
            };

            var firstMessageEvicted = false;
            var secondMessageEvicted = false;
            var thirdMessageEvicted = false;

            server.QueuedApplicationMessageOverwrittenAsync += args =>
            {
                if (args.Packet is not MqttPublishPacket publishPacket)
                {
                    return Task.CompletedTask;
                }

                switch (publishPacket.Topic)
                {
                    case "InjectedOne":
                        firstMessageEvicted = true;
                        break;
                    case "InjectedTwo":
                        secondMessageEvicted = true;
                        break;
                    case "InjectedThree":
                        thirdMessageEvicted = true;
                        break;
                }

                return Task.CompletedTask;
            };

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];
            await receiver.SubscribeAsync("#");

            var firstMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build(), out _);
            await firstMessageOutboundPacketInterceptedTcs.Task;

            var secondMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedTwo").Build(), out _);

            var thirdMessageEnqueued = clientStatus.TryEnqueueApplicationMessage(
                new MqttApplicationMessageBuilder().WithTopic("InjectedThree").Build(), out _);

            // Due to the DropOldestQueuedMessage strategy, all messages will be enqueued initially.
            // But the second message will eventually be dropped (evicted) to make room for the third one.
            Assert.IsTrue(firstMessageEnqueued);
            Assert.IsTrue(secondMessageEnqueued);
            Assert.IsTrue(thirdMessageEnqueued);

            Assert.IsFalse(firstMessageEvicted);
            Assert.IsTrue(secondMessageEvicted);
            Assert.IsFalse(thirdMessageEvicted);
        }

        [TestMethod]
        public async Task Deliver_Application_Message_At_Session_Level()
        {
            using var testEnvironment = CreateTestEnvironment();

            var server = await testEnvironment.StartServer();
            var receiver1 = await testEnvironment.ConnectClient();
            var receiver2 = await testEnvironment.ConnectClient();
            var messageReceivedHandler1 = testEnvironment.CreateApplicationMessageHandler(receiver1);
            var messageReceivedHandler2 = testEnvironment.CreateApplicationMessageHandler(receiver2);

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];

            await receiver1.SubscribeAsync("#");
            await receiver2.SubscribeAsync("#");

            var mqttApplicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("InjectedOne")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            var publishPacket = await clientStatus.DeliverApplicationMessageAsync(mqttApplicationMessage);

            await LongTestDelay();

            Assert.AreEqual(1, messageReceivedHandler1.ReceivedEventArgs.Count);
            Assert.AreEqual(publishPacket.PacketIdentifier, messageReceivedHandler1.ReceivedEventArgs[0].PacketIdentifier);
            Assert.AreEqual("InjectedOne", messageReceivedHandler1.ReceivedEventArgs[0].ApplicationMessage.Topic);

            // The second receiver should NOT receive the message.
            Assert.AreEqual(0, messageReceivedHandler2.ReceivedEventArgs.Count);
        }

        [TestMethod]
        public async Task Deliver_Application_Message_At_Session_Level_QueueOverflow_DropNewMessageStrategy()
        {
            using var testEnvironment = CreateTestEnvironment();

            var server = await testEnvironment.StartServer(
                builder => builder
                    .WithMaxPendingMessagesPerClient(1)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage));

            var receiver = await testEnvironment.ConnectClient();

            var firstMessageOutboundPacketInterceptedTcs = new TaskCompletionSource();
            server.InterceptingOutboundPacketAsync += async args =>
            {
                // - The first message is dequeued normally and calls this delay
                // - The second message fills the outbound queue
                // - The third message overflows the outbound queue
                if (args.Packet is MqttPublishPacket)
                {
                    firstMessageOutboundPacketInterceptedTcs.SetResult();
                    await Task.Delay(TimeSpan.FromDays(1), args.CancellationToken);
                }
            };

            var firstMessageEvicted = false;
            var secondMessageEvicted = false;
            var thirdMessageEvicted = false;

            server.QueuedApplicationMessageOverwrittenAsync += args =>
            {
                if (args.Packet is not MqttPublishPacket publishPacket)
                {
                    return Task.CompletedTask;
                }

                switch (publishPacket.Topic)
                {
                    case "InjectedOne":
                        firstMessageEvicted = true;
                        break;
                    case "InjectedTwo":
                        secondMessageEvicted = true;
                        break;
                    case "InjectedThree":
                        thirdMessageEvicted = true;
                        break;
                }

                return Task.CompletedTask;
            };

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];
            await receiver.SubscribeAsync("#");

            var firstMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build()));
            await LongTestDelay();
            await firstMessageOutboundPacketInterceptedTcs.Task;

            var secondMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedTwo").Build()));
            await LongTestDelay();

            var thirdMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedThree").Build()));
            await LongTestDelay();

            Task.WaitAny(firstMessageTask, secondMessageTask, thirdMessageTask);

            // Due to the DropNewMessage strategy the third message delivery will fail.
            // As a result, no existing messages in the queue will be dropped (evicted).
            Assert.AreEqual(firstMessageTask.Status, TaskStatus.WaitingForActivation);
            Assert.AreEqual(secondMessageTask.Status, TaskStatus.WaitingForActivation);
            Assert.AreEqual(thirdMessageTask.Status, TaskStatus.Faulted);
            Assert.IsTrue(thirdMessageTask.Exception?.InnerException is MqttPendingMessagesOverflowException);

            Assert.IsFalse(firstMessageEvicted);
            Assert.IsFalse(secondMessageEvicted);
            Assert.IsFalse(thirdMessageEvicted);
        }

        [TestMethod]
        public async Task Deliver_Application_Message_At_Session_Level_QueueOverflow_DropOldestQueuedMessageStrategy()
        {
            using var testEnvironment = CreateTestEnvironment(trackUnobservedTaskException: false);

            var server = await testEnvironment.StartServer(
                builder => builder
                    .WithMaxPendingMessagesPerClient(1)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage));

            var receiver = await testEnvironment.ConnectClient();

            var firstMessageOutboundPacketInterceptedTcs = new TaskCompletionSource();
            server.InterceptingOutboundPacketAsync += async args =>
            {
                // - The first message is dequeued normally and calls this delay
                // - The second message fills the outbound queue
                // - The third message overflows the outbound queue
                if (args.Packet is MqttPublishPacket)
                {
                    firstMessageOutboundPacketInterceptedTcs.SetResult();
                    await Task.Delay(TimeSpan.FromDays(1), args.CancellationToken);
                }
            };

            var firstMessageEvicted = false;
            var secondMessageEvicted = false;
            var thirdMessageEvicted = false;

            server.QueuedApplicationMessageOverwrittenAsync += args =>
            {
                if (args.Packet is not MqttPublishPacket publishPacket)
                {
                    return Task.CompletedTask;
                }

                switch (publishPacket.Topic)
                {
                    case "InjectedOne":
                        firstMessageEvicted = true;
                        break;
                    case "InjectedTwo":
                        secondMessageEvicted = true;
                        break;
                    case "InjectedThree":
                        thirdMessageEvicted = true;
                        break;
                }

                return Task.CompletedTask;
            };

            var status = await server.GetSessionsAsync();
            var clientStatus = status[0];
            await receiver.SubscribeAsync("#");

            var firstMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build()));
            await LongTestDelay();
            await firstMessageOutboundPacketInterceptedTcs.Task;

            var secondMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedTwo").Build()));
            await LongTestDelay();

            var thirdMessageTask = Task.Run(
                () => clientStatus.DeliverApplicationMessageAsync(
                    new MqttApplicationMessageBuilder().WithTopic("InjectedThree").Build()));
            await LongTestDelay();

            Task.WaitAny(firstMessageTask, secondMessageTask, thirdMessageTask);

            // Due to the DropOldestQueuedMessage strategy, the second message delivery will fail
            // to make room for the third one.
            Assert.AreEqual(firstMessageTask.Status, TaskStatus.WaitingForActivation);
            Assert.AreEqual(secondMessageTask.Status, TaskStatus.Faulted);
            Assert.IsTrue(secondMessageTask.Exception?.InnerException is MqttPendingMessagesOverflowException);
            Assert.AreEqual(thirdMessageTask.Status, TaskStatus.WaitingForActivation);

            Assert.IsFalse(firstMessageEvicted);
            Assert.IsTrue(secondMessageEvicted);
            Assert.IsFalse(thirdMessageEvicted);
        }

        [TestMethod]
        public async Task Inject_ApplicationMessage_At_Server_Level()
        {
            using var testEnvironment = CreateTestEnvironment();

            var server = await testEnvironment.StartServer();

            var receiver = await testEnvironment.ConnectClient();

            var messageReceivedHandler = testEnvironment.CreateApplicationMessageHandler(receiver);

            await receiver.SubscribeAsync("#");

            var injectedApplicationMessage = new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build();

            await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(injectedApplicationMessage));

            await LongTestDelay();

            Assert.AreEqual(1, messageReceivedHandler.ReceivedEventArgs.Count);
            Assert.AreEqual("InjectedOne", messageReceivedHandler.ReceivedEventArgs[0].ApplicationMessage.Topic);
        }

        [TestMethod]
        public async Task Intercept_Injected_Application_Message()
        {
            using var testEnvironment = CreateTestEnvironment();

            var server = await testEnvironment.StartServer();

            MqttApplicationMessage interceptedMessage = null;
            server.InterceptingPublishAsync += eventArgs =>
            {
                interceptedMessage = eventArgs.ApplicationMessage;
                return CompletedTask.Instance;
            };

            var injectedApplicationMessage = new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build();
            await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(injectedApplicationMessage));

            await LongTestDelay();

            Assert.IsNotNull(interceptedMessage);
        }
    }
}