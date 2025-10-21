// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Formatter;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[MemoryDiagnoser]
public class MqttBufferReaderBenchmark
{
    byte[] _buffer;
    int _bufferLength;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var writer = new MqttBufferWriter(1024, 1024);
        writer.WriteString("hgfjkdfkjlghfdjghdfljkdfhgdlkjfshgsldkfjghsdflkjghdsflkjhrstiuoghlkfjbhnfbutghjoiöjhklötnbhtroliöuhbjntluiobkjzbhtdrlskbhtruhjkfthgbkftgjhgfiklhotriuöhbjtrsioöbtrsötrhträhtrühjtriüoätrhjtsrölbktrbnhtrulöbionhströloubinströoliubhnsöotrunbtöroisntröointröioujhgötiohjgötorshjnbgtöorihbnjtröoihbjntröobntröoibntrjhötrohjbtröoihntröoibnrtoiöbtrjnboöitrhjtnriohötrhjtöroihjtroöihjtroösibntsroönbotöirsbntöoihjntröoihntroöbtrboöitrnhoöitrhjntröoishbnjtröosbhtröbntriohjtröoijtöoitbjöotibjnhöotirhbjntroöibhnjrtoöibnhtroöibnhtörsbnhtöoirbnhtöroibntoörhjnbträöbtrbträbtrbtirbätrsibohjntrsöiobthnjiohjsrtoib");

        _buffer = writer.GetBuffer();
        _bufferLength = writer.Length;
    }

    [Benchmark]
    public void Use_Span()
    {
        var span = _buffer.AsSpan(0, _bufferLength);
        Encoding.UTF8.GetString(span);
    }

    [Benchmark]
    public void Use_Encoding()
    {
        Encoding.UTF8.GetString(_buffer, 0, _bufferLength);
    }
}