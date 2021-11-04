using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public class Subscription_TopicHash_Tests
    {
        enum TopicHashSelector {  NoWildcard, SingleWildcard, MultiWildcard };

        MQTTnet.Server.MqttSession _clientSession;

        [TestMethod]
        public void Match_Hash_Test_SingleWildCard()
        {
            var l0 = "pub0";
            var l1 = "topic1";
            var l2 = "+";
            var l3 = "prop1";
            var topic = string.Format("{0}/{1}/{2}/{3}", l0, l1, l2, l3);


            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;
            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");

            var hashBytes = GetBytes(topicHash);
            Assert.AreNotEqual(hashBytes[0], 0, "checksum 0 mismatch");
            Assert.AreNotEqual(hashBytes[1], 0, "checksum 1 mismatch");
            Assert.AreEqual(hashBytes[2], 0, "checksum 2 mismatch");
            Assert.AreNotEqual(hashBytes[3], 0, "checksum 3 mismatch");
            Assert.AreEqual(hashBytes[4], 0, "checksum 4 mismatch");
            Assert.AreEqual(hashBytes[5], 0, "checksum 5 mismatch");
            Assert.AreEqual(hashBytes[6], 0, "checksum 6 mismatch");
            Assert.AreEqual(hashBytes[7], 0, "checksum 7 mismatch");

            // The mask should have zeroes where the wildcard and ff at the end
            var hashMaskBytes = GetBytes(topicHashMask);
            Assert.AreEqual(hashMaskBytes[0], 0xff, "mask 0 mismatch");
            Assert.AreEqual(hashMaskBytes[1], 0xff, "mask 1 mismatch");
            Assert.AreEqual(hashMaskBytes[2], 0, "mask 2 mismatch");
            Assert.AreEqual(hashMaskBytes[3], 0xff, "mask 3 mismatch");
            Assert.AreEqual(hashMaskBytes[4], 0xff, "mask 4 mismatch");
            Assert.AreEqual(hashMaskBytes[5], 0xff, "mask 5 mismatch");
            Assert.AreEqual(hashMaskBytes[6], 0xff, "mask 6 mismatch");
            Assert.AreEqual(hashMaskBytes[7], 0xff, "mask 7 mismatch");
        }

        [TestMethod]
        public void Match_Hash_Test_MultiWildCard()
        {
            var l0 = "pub0";
            var l1 = "topic1";
            var l2 = "sub1";
            var l3 = "#";
            var topic = string.Format("{0}/{1}/{2}/{3}", l0, l1, l2, l3);

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");

            var hashBytes = GetBytes(topicHash);
            Assert.AreNotEqual(hashBytes[0], 0, "checksum 0 mismatch");
            Assert.AreNotEqual(hashBytes[1], 0, "checksum 1 mismatch");
            Assert.AreNotEqual(hashBytes[2], 0, "checksum 2 mismatch");
            Assert.AreEqual(hashBytes[3], 0, "checksum 3 mismatch");
            Assert.AreEqual(hashBytes[4], 0, "checksum 4 mismatch");
            Assert.AreEqual(hashBytes[5], 0, "checksum 5 mismatch");
            Assert.AreEqual(hashBytes[6], 0, "checksum 6 mismatch");
            Assert.AreEqual(hashBytes[7], 0, "checksum 7 mismatch");

            // The mask should have zeroes where the wildcard is and zero onward
            var hashMaskBytes = GetBytes(topicHashMask);
            Assert.AreEqual(hashMaskBytes[0], 0xff, "mask 0 mismatch");
            Assert.AreEqual(hashMaskBytes[1], 0xff, "mask 1 mismatch");
            Assert.AreEqual(hashMaskBytes[2], 0xff, "mask 2 mismatch");
            Assert.AreEqual(hashMaskBytes[3], 0, "mask 3 mismatch");
            Assert.AreEqual(hashMaskBytes[4], 0, "mask 4 mismatch");
            Assert.AreEqual(hashMaskBytes[5], 0, "mask 5 mismatch");
            Assert.AreEqual(hashMaskBytes[6], 0, "mask 6 mismatch");
            Assert.AreEqual(hashMaskBytes[7], 0, "mask 7 mismatch");
        }

        [TestMethod]
        public void Match_Hash_Test_NoWildCard()
        {
            var l0 = "pub0";
            var l1 = "topic1";
            var l2 = "sub1";
            var l3 = "prop1";
            var topic = string.Format("{0}/{1}/{2}/{3}", l0, l1, l2, l3);

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsFalse(topicHasWildcard, "Wildcard detected when not wildcard present");


            var hashBytes = GetBytes(topicHash);
            Assert.AreNotEqual(hashBytes[0], 0, "checksum 0 mismatch");
            Assert.AreNotEqual(hashBytes[1], 0, "checksum 1 mismatch");
            Assert.AreNotEqual(hashBytes[2], 0, "checksum 2 mismatch");
            Assert.AreNotEqual(hashBytes[3], 0, "checksum 3 mismatch");
            Assert.AreEqual(hashBytes[4], 0, "checksum 4 mismatch");
            Assert.AreEqual(hashBytes[5], 0, "checksum 5 mismatch");
            Assert.AreEqual(hashBytes[6], 0, "checksum 6 mismatch");
            Assert.AreEqual(hashBytes[7], 0, "checksum 7 mismatch");

            // The mask should have ff 
            var hashMaskBytes = GetBytes(topicHashMask);
            Assert.AreEqual(hashMaskBytes[0], 0xff, "mask 0 mismatch");
            Assert.AreEqual(hashMaskBytes[1], 0xff, "mask 1 mismatch");
            Assert.AreEqual(hashMaskBytes[2], 0xff, "mask 2 mismatch");
            Assert.AreEqual(hashMaskBytes[3], 0xff, "mask 3 mismatch");
            Assert.AreEqual(hashMaskBytes[4], 0xff, "mask 4 mismatch");
            Assert.AreEqual(hashMaskBytes[5], 0xff, "mask 5 mismatch");
            Assert.AreEqual(hashMaskBytes[6], 0xff, "mask 6 mismatch");
            Assert.AreEqual(hashMaskBytes[7], 0xff, "mask 7 mismatch");
        }

        /// <summary>
        /// Test long topic name with last level being #
        /// </summary>
        [TestMethod]
        public void Match_Hash_Test_LongTopic_MultiWildcard()
        {
            var sb = new StringBuilder();
            const int NumLevels = 8;
            var levelNames = new string[NumLevels];
            for (var i = 0; i < NumLevels; ++i)
            {
                if (i > 0)
                    sb.Append("/");
                string levelName;
                if (i == NumLevels - 1)
                {
                    // last one is #
                    levelName = "#";
                }
                else
                {
                   levelName = "level" + i;
                }
                levelNames[i] = levelName;
                sb.Append(levelName);
            }
            var topic = sb.ToString();
            
            // UInt64 is limited to 8 levels

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");

            var hashBytes = GetBytes(topicHash);
            // all bytes should contain checksum
            int count = 0;
            foreach (var h in hashBytes)
            {
                if (count < 7)
                {
                    Assert.AreNotEqual(h, 0, "checksum mismatch");
                }
                else
                {
                    Assert.AreEqual(h, 0, "checksum mismatch");
                }
                ++count;
            }
            // The mask should have ff except for last level
            var hashMaskBytes = GetBytes(topicHashMask);
            count = 0;
            foreach (var h in hashMaskBytes)
            {
                if (count < 7)
                {
                    Assert.AreEqual(h, 0xff, "mask mismatch");
                }
                else
                {
                    Assert.AreEqual(h, 0, "last mask mismatch");
                }
                ++count;
            }
        }

        /// <summary>
        /// Test long topic name with last level being +
        /// </summary>
        [TestMethod]
        public void Match_Hash_Test_LongTopic_SingleWildCard()
        {
            var sb = new StringBuilder();
            const int NumLevels = 8;
            var levelNames = new string[NumLevels];
            for (var i = 0; i < NumLevels; ++i)
            {
                if (i > 0)
                    sb.Append("/");
                string levelName;
                if (i == NumLevels - 1)
                {
                    // last one is +
                    levelName = "+";
                }
                else
                {
                    levelName = "level" + i;
                }
                levelNames[i] = levelName;
                sb.Append(levelName);
            }
            var topic = sb.ToString();

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");


            var hashBytes = GetBytes(topicHash);
            // all bytes should contain checksum
            int count = 0;
            foreach (var h in hashBytes)
            {
                if (count < 7)
                {
                    Assert.AreNotEqual(h, 0, "checksum mismatch");
                }
                else
                {
                    // wildcard position
                    Assert.AreEqual(h, 0, "checksum mismatch");
                }
                ++count;
            }
            // The mask should have ff 
            var hashMaskBytes = GetBytes(topicHashMask);
            count = 0;
            foreach (var h in hashMaskBytes)
            {
                if (count < 7)
                {
                    Assert.AreEqual(h, 0xff, "mask mismatch");
                }
                else
                {
                    Assert.AreEqual(h, 0, "last mask mismatch");
                }
                ++count;
            }
        }

        /// <summary>
        /// Test long topic name exceeding 8 levels
        /// </summary>
        [TestMethod]
        public void Match_Hash_Test_LongTopic_NoWildCard()
        {
            var sb = new StringBuilder();
            const int NumLevels = 9;
            var levelNames = new string[NumLevels];
            for(var i=0; i < NumLevels; ++i)
            {
                if (i > 0)
                    sb.Append("/");
                var levelName = "level" + i;
                levelNames[i] = levelName;
                sb.Append(levelName);
            }
            var topic = sb.ToString();

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsFalse(topicHasWildcard, "Wildcard detected when not present");


            var hashBytes = GetBytes(topicHash);
            // all bytes should contain checksum
            int count = 0;
            foreach(var h in hashBytes)
            {
                Assert.AreNotEqual(h, 0, "checksum mismatch");
                ++count;
            }
            // The mask should have ff 
            var hashMaskBytes = GetBytes(topicHashMask);
            count = 0;
            foreach (var h in hashMaskBytes)
            {
                Assert.AreEqual(h, 0xff, "mask mismatch");
                ++count;
            }
        }


        /// <summary>
        /// Test long topic name with last level being +
        /// </summary>
        [TestMethod]
        public void Match_Hash_Test_LongTopic_DetectSingleLevelWildcard()
        {
            var topic = "asdfasdf/asdfasdf/asdfasdf/asdfasdf/asdfas/dfaf/assfdgsdfgdf/+";

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");
        }

        /// <summary>
        /// Test long topic name with last level being #
        /// </summary>
        [TestMethod]
        public void Match_Hash_Test_LongTopic_DetectMultiLevelWildcard()
        {
            var topic = "asdfasdf/asdfasdf/asdfasdf/asdfasdf/asdfas/dfaf/assfdgsdfgdf/#";

            UInt64 topicHash;
            UInt64 topicHashMask;
            bool topicHasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out topicHasWildcard);

            Assert.IsTrue(topicHasWildcard, "Wildcard not detected");
        }

        [TestMethod]
        public async Task Match_Hash_Test_Search_NoWildcard()
        {
            var topics = await PrepareTopicHashSubscriptions(TopicHashSelector.NoWildcard);

            // all match lookup
            var matchCount = CheckTopicSubscriptions(_clientSession, topics, "No Wildcard All Match");

            Assert.AreEqual(topics.Count, matchCount, "Not all topics matched");

            // no match lookup
            {
                {
                    var topicsToFind = new List<string>();
                    foreach (var t in topics)
                    {
                        topicsToFind.Add(t + "x");
                    }

                    matchCount = CheckTopicSubscriptions(_clientSession, topicsToFind, "No Wildcard No Match (Append X)");

                    Assert.AreEqual(0, matchCount, "Topic match count not zero");
                }

                {
                    var topicsToFind = new List<string>();
                    foreach (var t in topics)
                    {
                        // replace last letter with x
                        topicsToFind.Add(t.Substring(0, t.Length - 1) + "x");
                    }

                    matchCount = CheckTopicSubscriptions(_clientSession, topicsToFind, "No Wildcard No Match (Replace X)");

                    Assert.AreEqual(0, matchCount, "Topic match count not zero");
                }
            }
        }

        [TestMethod]
        public async Task Match_Hash_Test_Search_SingleWildcard()
        {
            var topics = await PrepareTopicHashSubscriptions(TopicHashSelector.SingleWildcard);
            var matchCount = CheckTopicSubscriptions(_clientSession, topics, "Single Wildcard");
            // Should match all topics
            Assert.AreEqual(topics.Count, matchCount, "Topics not matched");
        }

        [TestMethod]
        public async Task Match_Hash_Test_Search_MultiWildcard()
        {
            var topics = await PrepareTopicHashSubscriptions(TopicHashSelector.MultiWildcard);
            var matchCount = CheckTopicSubscriptions(_clientSession, topics, "Multi Wildcard");
            // Should match all topics
            Assert.AreEqual(topics.Count, matchCount, "Topics not matched");
        }

        /// <summary>
        /// Even fairly regularly named topics as generated by the topic generator should result in shallow hash buckets
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public void Check_Hash_Bucket_Depth()
        {
            Dictionary<string, List<string>> topicsByPublisher;
            Dictionary<string, List<string>> singleWildcardTopicsByPublisher;
            Dictionary<string, List<string>> multiWildcardTopicsByPublisher;

            const int NumPublishers = 5000;
            const int NumTopicsPerPublisher = 10;

            TopicGenerator.Generate(NumPublishers, NumTopicsPerPublisher, out topicsByPublisher, out singleWildcardTopicsByPublisher, out multiWildcardTopicsByPublisher);

            // There will be many 'similar' topics ending with, i.e. "sensor100", "sensor101", ...
            // Hash bucket depths should remain low.
            var bucketDepths = new Dictionary<UInt64, int>();
            int maxBucketDepth = 0;
            UInt64 maxBucketDepthHash = 0;

            var topicsByHash = new Dictionary<UInt64, List<string>>();

            foreach (var t in topicsByPublisher)
            {
                var topics = t.Value;
                foreach (var topic in topics)
                {
                    UInt64 topicHash;
                    UInt64 hashMask;
                    bool hasWildcard;
                    MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out hashMask, out hasWildcard);

                    bucketDepths.TryGetValue(topicHash, out var currentValue);
                    ++currentValue;
                    bucketDepths[topicHash] = currentValue;

                    if (currentValue > maxBucketDepth)
                    {
                        maxBucketDepth = currentValue;
                        maxBucketDepthHash = topicHash;
                    }

                    if (!topicsByHash.TryGetValue(topicHash, out var topicList))
                    {
                        topicList = new List<string>();
                        topicsByHash.Add(topicHash, topicList);
                    }
                    topicList.Add(topic);
                }
            }

            var maxDepthTopics = topicsByHash[maxBucketDepthHash];

            Console.Write("Max bucket depth is " + maxBucketDepth);

            // for the test case the bucket depth should be less than 100
            Assert.IsTrue(maxBucketDepth < 100, "Unexpected high topic hash bucket depth");
        }


        [TestMethod]
        public void Check_Selected_Topic_Hashes()
        {
            CheckTopicHash("client1/building1/level1/sensor1", 0x655D4AF100000000, 0xFFFFFFFFFFFFFFFF);
            CheckTopicHash("client1/building1/+/sensor1", 0x655D00F100000000, 0xFFFF00FFFFFFFFFF);
            CheckTopicHash("client1/+/level1/+", 0x65004A0000000000, 0xFF00FF00FFFFFFFF);
            CheckTopicHash("client1/building1/level1/#", 0x655D4A0000000000, 0xFFFFFF0000000000);
            CheckTopicHash("client1/+/level1/#", 0x65004A0000000000, 0xFF00FF0000000000);
        }

        void CheckTopicHash(string topic, ulong expectedHash, ulong expectedHashMask)
        {
            ulong topicHash;
            ulong hashMask;
            bool hasWildcard;

            MQTTnet.Server.MqttSubscription.CalcTopicHash(topic, out topicHash, out hashMask, out hasWildcard);

            Console.WriteLine();
            Console.WriteLine("Topic: " + topic);
            Console.WriteLine(string.Format("Hash: {0:X8}", topicHash));
            Console.WriteLine(string.Format("Hash Mask: {0:X8}", hashMask));

            Assert.AreEqual(expectedHash, topicHash, "Topic hash not as expected. Has the hash function changed?");
            Assert.AreEqual(expectedHashMask, hashMask, "Topic hash mask not as expected");
        }


        async Task<List<string>> PrepareTopicHashSubscriptions(TopicHashSelector selector)
        {
            Dictionary<string, List<string>> topicsByPublisher;
            Dictionary<string, List<string>> singleWildcardTopicsByPublisher;
            Dictionary<string, List<string>> multiWildcardTopicsByPublisher;

            const int NumPublishers = 1;
            const int NumTopicsPerPublisher = 10000;

            TopicGenerator.Generate(NumPublishers, NumTopicsPerPublisher, out topicsByPublisher, out singleWildcardTopicsByPublisher, out multiWildcardTopicsByPublisher);

            var topics = topicsByPublisher.FirstOrDefault().Value;
            var singleWildcardTopics = singleWildcardTopicsByPublisher.FirstOrDefault().Value;
            var multiWildcardTopics = multiWildcardTopicsByPublisher.FirstOrDefault().Value;

            const string ClientId = "Client1";
            var logger = new Mockups.TestLogger();
            var serverOptions = new MQTTnet.Server.MqttServerOptions();
            var eventContainer = new MQTTnet.Server.MqttServerEventContainer();
            var retainedMessagesManager = new MqttRetainedMessagesManager(eventContainer, logger);
            var sessionManager = new MqttClientSessionsManager(serverOptions, retainedMessagesManager, eventContainer, logger);
            _clientSession = new MQTTnet.Server.MqttSession(
                        ClientId,
                        new Dictionary<object, object>(),
                        serverOptions,
                        eventContainer,
                        retainedMessagesManager,
                        sessionManager
                        );

            List<string> topicsToSubscribe;

            switch (selector)
            {
                case TopicHashSelector.SingleWildcard:
                    topicsToSubscribe = singleWildcardTopics;
                    break;
                case TopicHashSelector.MultiWildcard:
                    topicsToSubscribe = multiWildcardTopics;
                    break;
                default:
                    topicsToSubscribe = topics;
                    break;
            }
            foreach (var t in topicsToSubscribe)
            {
                var subPacket = new Packets.MqttSubscribePacket();
                var filter = new Packets.MqttTopicFilter();
                filter.Topic = t;
                subPacket.TopicFilters.Add(filter);
                await _clientSession.SubscriptionsManager.Subscribe(subPacket, default(CancellationToken));
            }

            return topics;
        }

        int CheckTopicSubscriptions(MQTTnet.Server.MqttSession clientSession, List<string> topicsToFind, string subject)
        {
            var matchCount = 0;

            {
                int resultCount = 0;

                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                var countUp = 0;
                var countDown = topicsToFind.Count - 1;
                for (; countUp < topicsToFind.Count; ++countUp, --countDown)
                {
                    var topicToFind = topicsToFind[countDown];

                    UInt64 topicHash;
                    UInt64 hashMask;
                    bool hasWildcard;
                    MQTTnet.Server.MqttSubscription.CalcTopicHash((string)topicToFind, out topicHash, out hashMask, out hasWildcard);

                    var result = clientSession.SubscriptionsManager.CheckSubscriptions((string)topicToFind, topicHash, MqttQualityOfServiceLevel.AtMostOnce, "OtherClient");
                    if (result.IsSubscribed)
                    {
                        ++matchCount;
                    }
                    ++resultCount;
                }

                stopWatch.Stop();

                Console.Write("Match count: " + matchCount + "; ");

                Console.WriteLine(subject + " lookup milliseconds: " + stopWatch.ElapsedMilliseconds);
            }

            return matchCount;
        }

        byte[] GetBytes(UInt64 value)
        {
            var bytes = BitConverter.GetBytes(value);
            // Ensure that highest byte comes first for comparison left to right
            if (BitConverter.IsLittleEndian)
                return bytes.Reverse().ToArray();
            return bytes;
        }
    }
}
