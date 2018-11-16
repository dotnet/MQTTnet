﻿using Microsoft.AspNetCore.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore.Tests.Mockups;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Serializer;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class MqttConnectionContextTest
    {
        [TestMethod]
        public async Task TestReceivePacketAsyncThrowsWhenReaderCompleted()
        {
            var serializer = new MqttPacketSerializer {};
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var ctx = new MqttConnectionContext(serializer, connection);

            pipe.Receive.Writer.Complete();

            await Assert.ThrowsExceptionAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
        }
        
        [TestMethod]
        public async Task TestParallelWrites()
        {
            var serializer = new MqttPacketSerializer { };
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var ctx = new MqttConnectionContext(serializer, connection);

            var tasks = Enumerable.Range(1, 10).Select(_ => Task.Run(async () => 
            {
                for (int i = 0; i < 100; i++)
                {
                    await ctx.SendPacketAsync(new MqttPublishPacket(), CancellationToken.None).ConfigureAwait(false);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
