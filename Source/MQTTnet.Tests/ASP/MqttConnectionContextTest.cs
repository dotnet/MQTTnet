// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using MQTTnet.AspNetCore;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.ASP.Mockups;
using MQTTnet.Tests.Helpers;

namespace MQTTnet.Tests.ASP;

[TestClass]
public class MqttConnectionContextTest
{
    [TestMethod]
    public async Task TestCorruptedConnectPacket()
    {
        var writer = new MqttBufferWriter(4096, 65535);
        var serializer = new MqttPacketFormatterAdapter(writer);
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        var ctx = new MqttConnectionContext(serializer, connection);

        await pipe.Receive.Writer.WriteAsync(writer.AddMqttHeader(MqttControlPacketType.Connect, []));

        await Assert.ThrowsExactlyAsync<MqttProtocolViolationException>(() => ctx.ReceivePacketAsync(CancellationToken.None));

        // the first exception should complete the pipes so if someone tries to use the connection after that it should throw immidiatly
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ctx.ReceivePacketAsync(CancellationToken.None)).ConfigureAwait(false);
    }

    // TODO: Fix test
    // [TestMethod]
    // public async Task TestEndpoint()
    // {
    //     var mockup = new ConnectionHandlerMockup();
    //
    //     using (var host = new WebHostBuilder().UseKestrel(kestrel => kestrel.ListenLocalhost(1883, listener => listener.Use((ctx, next) => mockup.OnConnectedAsync(ctx))))
    //                .UseStartup<Startup>()
    //                .ConfigureServices(
    //                    (hostContext, services) =>
    //                    {
    //                        services.AddHostedMqttServer(o => o.WithoutDefaultEndpoint());
    //                        services.AddSingleton<IMqttServerAdapter>(mockup);
    //                    })
    //                .Build())
    //
    //     using (var client = new MqttFactory().CreateMqttClient())
    //     {
    //         host.Start();
    //         await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build(), CancellationToken.None);
    //
    //         var ctx = await mockup.Context.Task;
    //
    //         var ep = IPEndPoint.Parse(ctx.Endpoint);
    //         Assert.IsNotNull(ep);
    //
    //         Assert.IsNotNull(ctx);
    //     }
    // }

    // COMMENTED OUT DUE TO DEAD LOCK? OR VERY VERY SLOW PERFORMANCE ON LOCAL DEV MACHINE. TEST WAS STILL RUNNING AFTER SEVERAL MINUTES!
    //[TestMethod]
    //public async Task TestParallelWrites()
    //{
    //    var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311);
    //    var pipe = new DuplexPipeMockup();
    //    var connection = new DefaultConnectionContext();
    //    connection.Transport = pipe;
    //    var ctx = new MqttConnectionContext(serializer, connection);

    //    var tasks = Enumerable.Range(1, 100).Select(_ => Task.Run(async () =>
    //    {
    //        for (int i = 0; i < 100; i++)
    //        {
    //            await ctx.SendPacketAsync(new MqttPublishPacket(), TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false);
    //        }
    //    }));

    //    await Task.WhenAll(tasks).ConfigureAwait(false);
    //}

    [TestMethod]
    public async Task TestLargePacket()
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        var ctx = new MqttConnectionContext(serializer, connection);

        await ctx.SendPacketAsync(new MqttPublishPacket { PayloadSegment = new byte[20_000] }, CancellationToken.None).ConfigureAwait(false);

        var readResult = await pipe.Send.Reader.ReadAsync();
        Assert.IsGreaterThan(20000, readResult.Buffer.Length);
    }

    [TestMethod]
    public async Task TestReceivePacketAsyncThrowsWhenReaderCompleted()
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        var ctx = new MqttConnectionContext(serializer, connection);

        await pipe.Receive.Writer.CompleteAsync();

        await Assert.ThrowsExactlyAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(CancellationToken.None)).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow(new byte[] { 0x10 })]
    [DataRow(new byte[] { 0x10, 0x80 })]
    [DataRow(new byte[] { 0x10, 0x0C, 0x00 })]
    public async Task TestReceivePacketAsyncThrowsWhenReaderCompletedWithIncompletePacket(byte[] packet)
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        using var ctx = new MqttConnectionContext(serializer, connection);

        await pipe.Receive.Writer.WriteAsync(packet).ConfigureAwait(false);
        await pipe.Receive.Writer.CompleteAsync().ConfigureAwait(false);

        // Bound the test if the receiver keeps reading the completed pipe instead of reporting EOF.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsExactlyAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(timeout.Token)).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow(new byte[] { 0xC0, 0x00, 0xC0, 0x00 })]
    [DataRow(new byte[] { 0xC0, 0x00, 0xC0, 0x00, 0x10 })]
    public async Task TestReceivePacketAsyncReadsBufferedPacketsBeforeReportingEof(byte[] packets)
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        using var ctx = new MqttConnectionContext(serializer, connection);

        await pipe.Receive.Writer.WriteAsync(packets).ConfigureAwait(false);
        await pipe.Receive.Writer.CompleteAsync().ConfigureAwait(false);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Assert.IsInstanceOfType<MqttPingReqPacket>(await ctx.ReceivePacketAsync(timeout.Token).ConfigureAwait(false));
        Assert.IsInstanceOfType<MqttPingReqPacket>(await ctx.ReceivePacketAsync(timeout.Token).ConfigureAwait(false));
        Assert.AreEqual(4L, ctx.BytesReceived);
        await Assert.ThrowsExactlyAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(timeout.Token)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestReceivePacketAsyncWaitsForRemainingBytesWhileReaderIsOpen()
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
        var pipe = new DuplexPipeMockup();
        var connection = new DefaultConnectionContext();
        connection.Transport = pipe;
        using var ctx = new MqttConnectionContext(serializer, connection);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await pipe.Receive.Writer.WriteAsync(new byte[] { 0xC0 }).ConfigureAwait(false);
        var receiveTask = ctx.ReceivePacketAsync(timeout.Token);
        Assert.IsFalse(receiveTask.IsCompleted);

        await pipe.Receive.Writer.WriteAsync(new byte[] { 0x00 }).ConfigureAwait(false);
        Assert.IsInstanceOfType<MqttPingReqPacket>(await receiveTask.ConfigureAwait(false));
        Assert.AreEqual(2L, ctx.BytesReceived);

        await pipe.Receive.Writer.CompleteAsync().ConfigureAwait(false);
        await Assert.ThrowsExactlyAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(timeout.Token)).ConfigureAwait(false);
    }

    sealed class Startup
    {
#pragma warning disable CA1822
        public void Configure(IApplicationBuilder app)
#pragma warning restore CA1822
        {
        }
    }
}