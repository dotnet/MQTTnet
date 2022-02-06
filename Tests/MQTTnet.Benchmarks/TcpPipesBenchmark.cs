// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MQTTnet.AspNetCore.Client.Tcp;

namespace MQTTnet.Benchmarks
{
    [MemoryDiagnoser]
    public class TcpPipesBenchmark
    {
        IDuplexPipe _client;
        IDuplexPipe _server;

        [GlobalSetup]
        public void Setup()
        {
            var server = new TcpListener(IPAddress.Any, 1883);

            server.Start(1);

            var task = Task.Run(() => server.AcceptSocket());

            var clientConnection = new TcpConnection(new IPEndPoint(IPAddress.Loopback, 1883));

            clientConnection.StartAsync().GetAwaiter().GetResult();
            _client = clientConnection.Transport;

            var serverConnection = new TcpConnection(task.GetAwaiter().GetResult());
            serverConnection.StartAsync().GetAwaiter().GetResult();
            _server = serverConnection.Transport;
        }


        [Benchmark]
        public async Task Send_10000_Chunks_Pipe()
        {
            var size = 5;
            var iterations = 10000;

            await Task.WhenAll(WriteAsync(iterations, size), ReadAsync(iterations, size));
        }

        async Task ReadAsync(int iterations, int size)
        {
            await Task.Yield();

            var expected = iterations * size;
            long read = 0;
            var input = _client.Input;

            while (read < expected)
            {
                var readresult = await input.ReadAsync(CancellationToken.None).ConfigureAwait(false);
                input.AdvanceTo(readresult.Buffer.End);
                read += readresult.Buffer.Length;
            }
        }

        async Task WriteAsync(int iterations, int size)
        {
            await Task.Yield();

            var output = _server.Output;

            for (var i = 0; i < iterations; i++)
            {
                await output.WriteAsync(new byte[size], CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
