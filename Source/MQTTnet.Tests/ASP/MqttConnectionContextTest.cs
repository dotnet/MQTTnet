// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.ASP.Mockups;
using MQTTnet.Tests.Helpers;

namespace MQTTnet.Tests.ASP
{
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

            await pipe.Receive.Writer.WriteAsync(writer.AddMqttHeader(MqttControlPacketType.Connect, Array.Empty<byte>()));

            await Assert.ThrowsExceptionAsync<MqttProtocolViolationException>(() => ctx.ReceivePacketAsync(CancellationToken.None));

            // the first exception should complete the pipes so if someone tries to use the connection after that it should throw immidiatly
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => ctx.ReceivePacketAsync(CancellationToken.None));
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

            await ctx.SendPacketAsync(new MqttPublishPacket { Payload = new ReadOnlySequence<byte>(new byte[20_000]) }, CancellationToken.None).ConfigureAwait(false);

            var readResult = await pipe.Send.Reader.ReadAsync();
            Assert.IsTrue(readResult.Buffer.Length > 20000);
        }

        [TestMethod]
        public async Task TestReceivePacketAsyncThrowsWhenReaderCompleted()
        {
            var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var ctx = new MqttConnectionContext(serializer, connection);

            pipe.Receive.Writer.Complete();

            await Assert.ThrowsExceptionAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(CancellationToken.None));
        }

        class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
            }
        }
    }
}