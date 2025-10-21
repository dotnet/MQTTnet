#if DEBUG

using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server;

// ReSharper disable InconsistentNaming

[TestClass]
public sealed class Load_Tests : BaseTestClass
{
    [TestMethod]
    public async Task Handle_100_000_Messages_In_Low_Level_Client()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var receivedMessages = 0;

        server.InterceptingPublishAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessages);
            return CompletedTask.Instance;
        };

        for (var i = 0; i < 100; i++)
        {
            _ = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        using var client = await testEnvironment.ConnectLowLevelClient();

                        await client.SendAsync(
                            new MqttConnectPacket
                            {
                                ClientId = "Handle_100_000_Messages_In_Low_Level_Client_" + Guid.NewGuid()
                            },
                            CancellationToken.None);

                        var packet = await client.ReceiveAsync(CancellationToken.None);

                        var connAckPacket = packet as MqttConnAckPacket;

                        Assert.IsNotNull(connAckPacket);
                        Assert.AreEqual(MqttConnectReasonCode.Success, connAckPacket.ReasonCode);

                        var publishPacket = new MqttPublishPacket();

                        for (var j = 0; j < 1000; j++)
                        {
                            publishPacket.Topic = j.ToString();

                            await client.SendAsync(publishPacket, CancellationToken.None);
                        }

                        await client.DisconnectAsync(CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                },
                TaskCreationOptions.LongRunning);
        }

        var result = SpinWait.SpinUntil(() => receivedMessages >= 100000, TimeSpan.FromMinutes(5));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_100_000_Messages_In_Receiving_Client()
    {
        using var testEnvironment = CreateTestEnvironment();
        await testEnvironment.StartServer();

        var receivedMessages = 0;

        using var receiverClient = await testEnvironment.ConnectClient();

        receiverClient.ApplicationMessageReceivedAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessages);
            return CompletedTask.Instance;
        };

        await receiverClient.SubscribeAsync("t/+");

        for (var i = 0; i < 100; i++)
        {
            _ = Task.Factory.StartNew(
                async () =>
                {
                    using var client = await testEnvironment.ConnectClient();
                    var applicationMessageBuilder = testEnvironment.ClientFactory.CreateApplicationMessageBuilder();

                    for (var j = 0; j < 1000; j++)
                    {
                        var message = applicationMessageBuilder.WithTopic("t/" + j).Build();

                        await client.PublishAsync(message);
                    }

                    await client.DisconnectAsync();
                },
                TaskCreationOptions.LongRunning);
        }

        var result = SpinWait.SpinUntil(() => receivedMessages >= 100000, TimeSpan.FromMinutes(5));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_100_000_Messages_In_Server()
    {
        using var testEnvironment = CreateTestEnvironment();
        var server = await testEnvironment.StartServer();

        var receivedMessages = 0;

        server.InterceptingPublishAsync += _ =>
        {
            Interlocked.Increment(ref receivedMessages);
            return CompletedTask.Instance;
        };

        for (var i = 0; i < 100; i++)
        {
            _ = Task.Factory.StartNew(
                async () =>
                {
                    using var client = await testEnvironment.ConnectClient();
                    var applicationMessageBuilder = new MqttApplicationMessageBuilder();

                    for (var j = 0; j < 1000; j++)
                    {
                        var message = applicationMessageBuilder.WithTopic(j.ToString()).Build();

                        await client.PublishAsync(message);
                    }

                    await client.DisconnectAsync();
                },
                TaskCreationOptions.LongRunning);
        }

        var result = SpinWait.SpinUntil(() => receivedMessages >= 100000, TimeSpan.FromMinutes(5));

        Assert.IsTrue(result);
    }
}

#endif