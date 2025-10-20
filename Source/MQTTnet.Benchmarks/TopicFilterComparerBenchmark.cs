// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MQTTnet.Benchmarks;

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter]
[MemoryDiagnoser]
public class TopicFilterComparerBenchmark : BaseBenchmark
{
    readonly string _longTopic =
        "AAAAAAAAAAAAAssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssshhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";

    [Benchmark]
    public void MqttTopicFilterComparer_10000_LoopMethod()
    {
        for (var i = 0; i < 100000; i++)
        {
            MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/#");
            MqttTopicFilterComparer.Compare("sport/tennis/player1/ranking", "sport/#/ranking");
            MqttTopicFilterComparer.Compare("sport/tennis/player1/score/wimbledon", "sport/+/player1/#");
            MqttTopicFilterComparer.Compare("sport/tennis/player1", "sport/tennis/+");
            MqttTopicFilterComparer.Compare("/finance", "+/+");
            MqttTopicFilterComparer.Compare("/finance", "/+");
            MqttTopicFilterComparer.Compare("/finance", "+");
            MqttTopicFilterComparer.Compare(_longTopic, _longTopic);
        }
    }
}