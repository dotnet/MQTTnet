// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Formatter;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [MemoryDiagnoser]
    public class MqttBufferReaderBenchmark
    {
        ArraySegment<byte> _buffer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var writer = new MqttBufferWriter(1024, 1024);
            writer.WriteString("hgfjkdfkjlghfdjghdfljkdfhgdlkjfshgsldkfjghsdflkjghdsflkjhrstiuoghlkfjbhnfbutghjoiöjhklötnbhtroliöuhbjntluiobkjzbhtdrlskbhtruhjkfthgbkftgjhgfiklhotriuöhbjtrsioöbtrsötrhträhtrühjtriüoätrhjtsrölbktrbnhtrulöbionhströloubinströoliubhnsöotrunbtöroisntröointröioujhgötiohjgötorshjnbgtöorihbnjtröoihbjntröobntröoibntrjhötrohjbtröoihntröoibnrtoiöbtrjnboöitrhjtnriohötrhjtöroihjtroöihjtroösibntsroönbotöirsbntöoihjntröoihntroöbtrboöitrnhoöitrhjntröoishbnjtröosbhtröbntriohjtröoijtöoitbjöotibjnhöotirhbjntroöibhnjrtoöibnhtroöibnhtörsbnhtöoirbnhtöroibntoörhjnbträöbtrbträbtrbtirbätrsibohjntrsöiobthnjiohjsrtoib");

            if (MemoryMarshal.TryGetArray(writer.GetWrittenMemory(), out var segment))
            {
                _buffer = segment;
            }
        }

        [Benchmark]
        public void Use_Span()
        {
            Encoding.UTF8.GetString(_buffer.AsSpan());
        }

        [Benchmark]
        public void Use_Encoding()
        {
            Encoding.UTF8.GetString(_buffer.Array, _buffer.Offset, _buffer.Count);
        }
    }
}