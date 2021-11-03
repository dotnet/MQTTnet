using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MQTTnet.TestApp.NetCore
{
    /// <summary>
    /// Connect a number of publisher clients and one subscriber client, then publish messages and measure
    /// the number of messages per second that can be exchanged between publishers and subscriber.
    /// Measurements are performed for subscriptions containing no wildcard, a single wildcard or multiple wildcards.
    /// </summary>
    public class MessageThroughputTest
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

        IMqttServer _mqttServer;
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

                await Subscribe_to_No_Wildcard_Topics();
                await Subscribe_to_Single_Wildcard_Topics();
                await Subscribe_to_Multi_Wildcard_Topics();

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
            new TopicGenerator().Generate(NumPublishers, NumTopicsPerPublisher, out _topicsByPublisher, out _singleWildcardTopicsByPublisher, out _multiWildcardTopicsByPublisher);

            var factory = new MqttFactory();
            _mqttServer = factory.CreateMqttServer();
            var serverOptions = new MqttServerOptionsBuilder().Build();
            await _mqttServer.StartAsync(serverOptions);

            Console.WriteLine();
            Console.WriteLine("Begin connect " + NumPublishers + " publisher(s)...");

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            _mqttPublisherClientsByPublisherName = new Dictionary<string, IMqttClient>();
            foreach (var pt in _topicsByPublisher)
            {
                var publisherName = pt.Key;
                var mqttClient = factory.CreateMqttClient();
                var publisherOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .WithClientId(publisherName)
                    .Build();
                await mqttClient.ConnectAsync(publisherOptions);
                _mqttPublisherClientsByPublisherName.Add(publisherName, mqttClient);
            }
            stopWatch.Stop();

            Console.Write(string.Format("{0} publisher(s) connected in {1:0.000} seconds, ", NumPublishers, stopWatch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine(string.Format("connections per second: {0:0.000}", NumPublishers / (stopWatch.ElapsedMilliseconds / 1000.0)));

            _mqttSubscriberClients = new List<IMqttClient>();
            for (var i = 0; i < NumSubscribers; ++i)
            {
                var mqttClient = factory.CreateMqttClient();
                var subsriberOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .WithClientId("sub" + i)
                    .Build();
                await mqttClient.ConnectAsync(subsriberOptions);
                mqttClient.ApplicationMessageReceivedHandler = new Client.Receiving.MqttApplicationMessageReceivedHandlerDelegate(r =>
                {
                    // count messages and signal cancellation when expected message count is reached
                    ++_messagesReceivedCount;
                    if (_messagesReceivedCount == _messagesExpectedCount)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                });
                _mqttSubscriberClients.Add(mqttClient);
            }
        }

        public async Task Cleanup()
        {
            foreach (var mqttClient in _mqttSubscriberClients)
            {
                await mqttClient.DisconnectAsync();
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
        public Task Subscribe_to_No_Wildcard_Topics()
        {
            return ProcessMessages(_topicsByPublisher, "no wildcards");
        }

        /// <summary>
        /// Measure single-wildcard topic subscription message exchange performance
        /// </summary>
        public Task Subscribe_to_Single_Wildcard_Topics()
        {
            return ProcessMessages(_singleWildcardTopicsByPublisher, "single wildcard");
        }

        /// <summary>
        /// Measure multi-wildcard topic subscription message exchange performance
        /// </summary>
        public Task Subscribe_to_Multi_Wildcard_Topics()
        {
            return ProcessMessages(_multiWildcardTopicsByPublisher, "multi wildcard");
        }


        /// <summary>
        ///  Subcribe to all topics, then run message exchange
        /// </summary>
        public async Task ProcessMessages(Dictionary<string, List<string>> topicsByPublisher, string topicTypeDescription)
        {
            var numTopics = CountTopics(topicsByPublisher);

            Console.WriteLine();
            Console.Write(string.Format("Subscribing to {0} topics ", numTopics));
            ConsoleWriteInfo(string.Format("({0})", topicTypeDescription));
            Console.WriteLine(string.Format(" for {0} subscriber(s)...", NumSubscribers));

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            // each subscriber subscribes to all topics, one call per topic
            foreach (var subscriber in _mqttSubscriberClients)
            {
                foreach (var tp in topicsByPublisher)
                {
                    var topics = tp.Value;
                    foreach (var topic in topics)
                    {
                        var topicFilter = new MqttTopicFilter() { Topic = topic };
                        await subscriber.SubscribeAsync(topicFilter).ConfigureAwait(false);
                    }
                }
            }

            stopWatch.Stop();

            Console.Write(string.Format("{0} subscriber(s) subscribed in {1:0.000} seconds, ", NumSubscribers, stopWatch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine(string.Format("subscribe calls per second: {0:0.000}", numTopics / (stopWatch.ElapsedMilliseconds / 1000.0)));

            await PublishAllAsync();

            Console.WriteLine(string.Format("Unsubscribing {0} topics ({1}) for {2} subscriber(s)...", numTopics, topicTypeDescription, NumSubscribers));

            stopWatch.Restart();

            // unsubscribe to all topics, one call per topic
            foreach (var subscriber in _mqttSubscriberClients)
            {
                foreach (var tp in topicsByPublisher)
                {
                    var topics = tp.Value;
                    foreach (var topic in topics)
                    {
                        var topicFilter = new MqttTopicFilter() { Topic = topic };

                        Client.Unsubscribing.MqttClientUnsubscribeOptions options =
                            new Client.Unsubscribing.MqttClientUnsubscribeOptionsBuilder()
                            .WithTopicFilter(topicFilter)
                            .Build();
                        await subscriber.UnsubscribeAsync(options).ConfigureAwait(false);
                    }
                }
            }

            stopWatch.Stop();

            Console.Write(string.Format("{0} subscriber(s) unsubscribed in {1:0.000} seconds, ", NumSubscribers, stopWatch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine(string.Format("unsubscribe calls per second: {0:0.000}", numTopics / (stopWatch.ElapsedMilliseconds / 1000.0)));
        }


        /// <summary>
        /// Publish messages in batches of NumPublishCallsPerBatch, wait for messages sent to subscriber
        /// </summary>
        async Task PublishAllAsync()
        {
            Console.WriteLine("Begin message exchange...");

            int publisherIndexCounter = 0;       // index to loop around all publishers to publish
            int topicIndexCounter = 0;           // index to loop around all topics to publish
            int totalNumMessagesReceived = 0;

            var publisherNames = _topicsByPublisher.Keys.ToList();

            // There should be one message received per publish
            _messagesExpectedCount = NumPublishCallsPerBatch;

            var stopWatch = new System.Diagnostics.Stopwatch();
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
                catch
                {

                }

                _cancellationTokenSource.Dispose();

                if (_messagesReceivedCount < _messagesExpectedCount)
                {
                    ConsoleWriteLineError(string.Format("Messages Received Count mismatch, expected {0}, received {1}", _messagesExpectedCount, _messagesReceivedCount));
                    return;
                }

                totalNumMessagesReceived += _messagesReceivedCount;
            }

            stopWatch.Stop();

            System.Console.Write(string.Format("{0} messages published and received in {1:0.000} seconds, ", totalNumMessagesReceived, stopWatch.ElapsedMilliseconds / 1000.0));
            ConsoleWriteLineSuccess(string.Format("messages per second: {0:0.000}", totalNumMessagesReceived / (stopWatch.ElapsedMilliseconds / 1000.0)));
        }

        int CountTopics(Dictionary<string, List<string>> topicsByPublisher)
        {
            var count = 0;
            foreach (var tp in topicsByPublisher)
            {
                count += tp.Value.Count;
            }
            return count;
        }

        void ConsoleWriteLineError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        void ConsoleWriteLineSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        void ConsoleWriteInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
            Console.ResetColor();
        }

    }
}
