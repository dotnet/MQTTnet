// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Internal;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class JsonPayloadBenchmark : BaseBenchmark
    {
        [Params(1 * 1024, 4 * 1024, 8 * 1024)]
        public int PayloadSize { get; set; }
        private Model model;


        [GlobalSetup]
        public void Setup()
        {
            var stringValue = new char[PayloadSize];
            model = new Model { StringValue = new string(stringValue) };
        }

        [Benchmark]
        public ValueTask SerializeToString_Payload_SendAsync()
        {
            string payload = JsonSerializer.Serialize(model);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("t")
                .WithPayload(payload)
                .Build();

            // send message async
            return ValueTask.CompletedTask;
        }

        [Benchmark]
        public ValueTask SerializeToUtf8Bytes_Payload_SendAsync()
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(model);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("t")
                .WithPayload(payload)
                .Build();

            // send message async
            return ValueTask.CompletedTask;
        }

        [Benchmark(Baseline = true)]
        public async ValueTask MqttPayloadOwnerFactory_Payload_SendAsync()
        {
            await using var payloadOwner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(async writer =>
               await JsonSerializer.SerializeAsync(writer.AsStream(leaveOpen: true), model));

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("t")
                .WithPayload(payloadOwner.Payload)
                .Build();

            // send message async
        }



        public class Model
        {
            public string StringValue { get; set; }

            public int IntValue { get; set; }

            public bool BoolValue { get; set; }

            public double DoubleValue { get; set; }
        }
    }
}
