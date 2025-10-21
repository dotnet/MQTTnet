using MQTTnet.Server;
using System.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.TestApp;

/// <summary>
/// Connect a number of publisher clients and one subscriber client, then publish messages and measure
/// the number of messages per second that can be exchanged between publishers and subscriber.
/// Measurements are performed for subscriptions containing no wildcard, a single wildcard or multiple wildcards.
/// </summary>
public sealed class MessageThroughputTest : IDisposable
{
    // Change these constants to suit
    const int NumPublishers = 5000;
    const int NumTopicsPerPublisher = 10;

    // Note: Other code changes may be required when changing this constant:
    const int NumSubscribers = 1; // Fixed

    // Number of publish calls before a response for all published messages is awaited.
    // This must be limited to a reasonable value that the server or TCP pipeline can handle.
    const int NumPublishCallsPerBatch = 250;

    // Message counters are set/reset in the PublishAllAsync loop
    int _messagesReceivedCount;
    int _messagesExpectedCount;

    CancellationTokenSource _cancellationTokenSource;

    MqttServer _mqttServer;
    Dictionary<string, IMqttClient> _mqttPublisherClientsByPublisherName;
    List<IMqttClient> _mqttSubscriberClients;

    Dictionary<string, List<string>> _topicsByPublisher;
    Dictionary<string, List<string>> _singleWildcardTopicsByPublisher;
    Dictionary<string, List<string>> _multiWildcardTopicsByPublisher;

    public async Task Run()
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("Begin message throughput test");
            Console.WriteLine();

            Console.WriteLine("Number of publishers: " + NumPublishers);
            Console.WriteLine("Number of published topics (total): " + NumPublishers * NumTopicsPerPublisher);
            Console.WriteLine("Number of subscribers: " + NumSubscribers);

            await Setup();

            await SubscribeToNoWildcardTopics();
            await SubscribeToSingleWildcardTopics();
            await SubscribeToMultiWildcardTopics();

            Console.WriteLine();
            Console.WriteLine("End message throughput test");
        }
        catch (Exception ex)
        {
            ConsoleWriteLineError(ex.Message);
        }
        finally
        {
            await Cleanup();
        }
    }

    public async Task Setup()
    {
        TopicGenerator.Generate(NumPublishers, NumTopicsPerPublisher, out _topicsByPublisher, out _singleWildcardTopicsByPublisher, out _multiWildcardTopicsByPublisher);

        var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
        var mqttClientFactory = new MqttClientFactory();
        var mqttServerFactory = new MqttServerFactory();
        _mqttServer = mqttServerFactory.CreateMqttServer(serverOptions);
        await _mqttServer.StartAsync();

        Console.WriteLine();
        Console.WriteLine("Begin connect " + NumPublishers + " publisher(s)...");

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        _mqttPublisherClientsByPublisherName = [];
        foreach (var pt in _topicsByPublisher)
        {
            var publisherName = pt.Key;
            var mqttClient = mqttClientFactory.CreateMqttClient();
            var publisherOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .WithClientId(publisherName)
                .Build();
            await mqttClient.ConnectAsync(publisherOptions);
            _mqttPublisherClientsByPublisherName.Add(publisherName, mqttClient);
        }
        stopWatch.Stop();

        Console.Write($"{NumPublishers} publisher(s) connected in {stopWatch.ElapsedMilliseconds / 1000.0:0.000} seconds, ");
        Console.WriteLine($"connections per second: {NumPublishers / (stopWatch.ElapsedMilliseconds / 1000.0):0.000}");

        _mqttSubscriberClients = new List<IMqttClient>();
        for (var i = 0; i < NumSubscribers; ++i)
        {
            var mqttClient = mqttClientFactory.CreateMqttClient();
            var subsriberOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .WithClientId("sub" + i)
                .Build();
            await mqttClient.ConnectAsync(subsriberOptions);
            mqttClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
            _mqttSubscriberClients.Add(mqttClient);
        }
    }

    Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        // count messages and signal cancellation when expected message count is reached
        ++_messagesReceivedCount;
        if (_messagesReceivedCount == _messagesExpectedCount)
        {
            _cancellationTokenSource.Cancel();
        }
        return CompletedTask.Instance;
    }

    public async Task Cleanup()
    {
        foreach (var mqttClient in _mqttSubscriberClients)
        {
            await mqttClient.DisconnectAsync();
            mqttClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;
            mqttClient.Dispose();
        }

        foreach (var pub in _mqttPublisherClientsByPublisherName)
        {
            var mqttClient = pub.Value;
            await mqttClient.DisconnectAsync();
            mqttClient.Dispose();
        }

        await _mqttServer.StopAsync();
        _mqttServer.Dispose();
    }

    /// <summary>
    /// Measure no-wildcard topic subscription message exchange performance
    /// </summary>
    public Task SubscribeToNoWildcardTopics()
    {
        return ProcessMessages(_topicsByPublisher, "no wildcards");
    }

    /// <summary>
    /// Measure single-wildcard topic subscription message exchange performance
    /// </summary>
    public Task SubscribeToSingleWildcardTopics()
    {
        return ProcessMessages(_singleWildcardTopicsByPublisher, "single wildcard");
    }

    /// <summary>
    /// Measure multi-wildcard topic subscription message exchange performance
    /// </summary>
    public Task SubscribeToMultiWildcardTopics()
    {
        return ProcessMessages(_multiWildcardTopicsByPublisher, "multi wildcard");
    }


    /// <summary>
    ///  Subscribe to all topics, then run message exchange
    /// </summary>
    public async Task ProcessMessages(Dictionary<string, List<string>> topicsByPublisher, string topicTypeDescription)
    {
        var numTopics = CountTopics(topicsByPublisher);

        Console.WriteLine();
        Console.Write($"Subscribing to {numTopics} topics ");
        ConsoleWriteInfo($"({topicTypeDescription})");
        Console.WriteLine($" for {NumSubscribers} subscriber(s)...");

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        // each subscriber subscribes to all topics, one call per topic
        foreach (var subscriber in _mqttSubscriberClients)
        {
            foreach (var tp in topicsByPublisher)
            {
                var topics = tp.Value;
                foreach (var topic in topics)
                {
                    var topicFilter = new Packets.MqttTopicFilter() { Topic = topic };
                    await subscriber.SubscribeAsync(topicFilter).ConfigureAwait(false);
                }
            }
        }

        stopWatch.Stop();

        Console.Write($"{NumSubscribers} subscriber(s) subscribed in {stopWatch.ElapsedMilliseconds / 1000.0:0.000} seconds, ");
        Console.WriteLine($"subscribe calls per second: {numTopics / (stopWatch.ElapsedMilliseconds / 1000.0):0.000}");

        await PublishAllAsync();

        Console.WriteLine($"Unsubscribing {numTopics} topics ({topicTypeDescription}) for {NumSubscribers} subscriber(s)...");

        stopWatch.Restart();

        // unsubscribe to all topics, one call per topic
        foreach (var subscriber in _mqttSubscriberClients)
        {
            foreach (var tp in topicsByPublisher)
            {
                var topics = tp.Value;
                foreach (var topic in topics)
                {
                    var topicFilter = new Packets.MqttTopicFilter { Topic = topic };

                    var options =
                        new MqttClientUnsubscribeOptionsBuilder()
                            .WithTopicFilter(topicFilter)
                            .Build();
                    await subscriber.UnsubscribeAsync(options).ConfigureAwait(false);
                }
            }
        }

        stopWatch.Stop();

        Console.Write($"{NumSubscribers} subscriber(s) unsubscribed in {stopWatch.ElapsedMilliseconds / 1000.0:0.000} seconds, ");
        Console.WriteLine($"unsubscribe calls per second: {numTopics / (stopWatch.ElapsedMilliseconds / 1000.0):0.000}");
    }

    /// <summary>
    /// Publish messages in batches of NumPublishCallsPerBatch, wait for messages sent to subscriber
    /// </summary>
    async Task PublishAllAsync()
    {
        Console.WriteLine("Begin message exchange...");

        var publisherIndexCounter = 0;       // index to loop around all publishers to publish
        var topicIndexCounter = 0;           // index to loop around all topics to publish
        var totalNumMessagesReceived = 0;

        var publisherNames = _topicsByPublisher.Keys.ToList();

        // There should be one message received per publish
        _messagesExpectedCount = NumPublishCallsPerBatch;

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        // Loop for a while and exchange messages

        while (stopWatch.ElapsedMilliseconds < 10000)
        {
            _messagesReceivedCount = 0;

            _cancellationTokenSource = new CancellationTokenSource();

            for (var publishCallCount = 0; publishCallCount < NumPublishCallsPerBatch; ++publishCallCount)
            {
                // pick a publisher
                var publisherName = publisherNames[publisherIndexCounter % publisherNames.Count];
                var publisherTopics = _topicsByPublisher[publisherName];
                // pick a publisher topic
                var topic = publisherTopics[topicIndexCounter % publisherTopics.Count];
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .Build();

                await _mqttPublisherClientsByPublisherName[publisherName].PublishAsync(message).ConfigureAwait(false);
                ++topicIndexCounter;
                ++publisherIndexCounter;
            }

            // Wait for at least one message per publish to be received by subscriber (in the subscriber's application message handler),
            // then loop around to send another batch
            try
            {
                await Task.Delay(30000, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Debug.Write(exception.ToString());
            }

            _cancellationTokenSource.Dispose();

            if (_messagesReceivedCount < _messagesExpectedCount)
            {
                ConsoleWriteLineError($"Messages Received Count mismatch, expected {_messagesExpectedCount}, received {_messagesReceivedCount}");
                return;
            }

            totalNumMessagesReceived += _messagesReceivedCount;
        }

        stopWatch.Stop();

        Console.Write($"{totalNumMessagesReceived} messages published and received in {stopWatch.ElapsedMilliseconds / 1000.0:0.000} seconds, ");
        ConsoleWriteLineSuccess($"messages per second: {(int)(totalNumMessagesReceived / (stopWatch.ElapsedMilliseconds / 1000.0))}");
    }

    static int CountTopics(Dictionary<string, List<string>> topicsByPublisher)
    {
        var count = 0;
        foreach (var tp in topicsByPublisher)
        {
            count += tp.Value.Count;
        }

        return count;
    }

    static void ConsoleWriteLineError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void ConsoleWriteLineSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    static void ConsoleWriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(message);
        Console.ResetColor();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}