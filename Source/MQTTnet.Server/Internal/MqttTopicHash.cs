// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server.Internal
{
    /*
    * The MqttSubscription object stores subscription parameters and calculates 
    * topic hashes.
    *
    * Use of Topic Hashes to improve message processing performance
    * =============================================================
    * 
    * Motivation
    * -----------
    * In a typical use case for MQTT there may be many publishers (sensors or 
    * other devices in the field) and few subscribers (monitoring all or many topics).
    * Each publisher may have one or more topic(s) to publish and therefore both, the 
    * number of publishers and the number of topics may be large.
    * 
    * Maintaining subscribers in a separate container
    * -----------------------------------------------
    * Instead of placing all sessions into a single _sessions container, subscribers 
    * are added into another _subscriberSessions container (if a client is a
    * subscriber and a publisher then the client is present in both containers). The
    * cost is some additional bookkeeping work upon subscription where each client 
    * session needs to maintain a list of subscribed topics.
    * 
    * When the client subscribes to the first topic, then the session manager adds
    * the client to the _subscriberSessions container, and when the client 
    * unsubscribes from the last topic then the session manager removes the client
    * from the container. Now, when an application message arrives, only the list of
    * subscribers need processing instead of looping through potentially thousands of
    * publishers.
    * 
    * Improving subscriber topic lookup
    * ---------------------------------
    * For each subscriber, it needs to be determined whether an application message 
    * matches any topic the subscriber has subscribed to. There may only be few 
    * subscribers but there may be many subscribed topics, including wildcard topics.
    * 
    * The implemented approach uses a topic hash and a hash mask calculated on the
    * subscribed topic and the published topic (the application message topic) to 
    * find candidates for a match, with the existing match logic evaluating a reduced
    * number of candidates.
    * 
    * For each subscription, the topic hash and a hash mask is stored with the
    * subscription, and for each application message received, the hash is calculated
    * for the published topic before attempting to find matching subscriptions. The
    * hash calculation itself is simple and does not have a large performance impact.
    * 
    * We'll first explain how topic hashes and hash masks are constructed and then how
    * they are used.
    * 
    * Topic hash
    * ----------
    * Topic hashes are stored as 64-bit numbers. Each byte within the 64-bit number
    * relates to one MQTT topic level. A checksum is calculated for each topic level
    * by iterating over the characters within the topic level (cast to byte) and the
    * result is stored into the corresponding byte of the 64-bit number. If a topic
    * level contains a wildcard character, then 0x00 is stored instead of the
    * checksum.
    * 
    * If there are less than 8 levels then the rest of the 64-bit number is filled
    * with 0xff. If there are more than 8 levels then topics where the first 8 MQTT
    * topic levels are identical will have the same hash value.
    * 
    * This is the topic hash for the MQTT topic below: 0x655D4AF1FFFFFF
    * 
    * client1/building1/level1/sensor1 (empty) (empty) (empty) (empty)
    * \_____/ \_______/ \____/ \_____/ \_____/ \_____/ \_____/ \_____/
    *    |        |       |       |       |       |       |       |
    *   0x65     0x5D    0x4A   0xF1     0xFF    0xFF    0xFF    0xFF
    * 
    * This is the topic hash for an MQTT topic containing a wildcard: 0x655D00F1FFFFFF
    * 
    * client1/building1/ + /sensor1 (empty) (empty) (empty) (empty)
    * \_____/ \_______/ \_/ \_____/ \_____/ \_____/ \_____/ \_____/
    *    |        |      |     |       |       |       |       |
    *   0x65     0x5D    0    0xF1    0xFF    0xFF    0xFF    0xFF
    * 
    * For topics that contain the multi level wildcard # at the end, the topic hash
    * is filled with 0x00: 0x65004A00000000
    * 
    * client1/ + /level1/ #  (empty) (empty) (empty) (empty)
    * \_____/ \_/ \____/ \_/ \_____/ \_____/ \_____/ \_____/
    *    |     |    |     |     |       |       |       |
    *   0x65   0   0x4A   0     0       0       0       0
    * 
    * 
    * Topic hash mask
    * ---------------
    * The hash mask simply contains 0xFF for non-wildcard topic levels and 0x00 for
    * wildcard topic levels. Here are the topic hash masks for the examples above.
    * 
    * client1/building1/level1/sensor1 (empty) (empty) (empty) (empty)
    * \_____/ \_______/ \____/ \_____/ \_____/ \_____/ \_____/ \_____/
    *    |        |       |       |       |       |       |       |
    *   0xFF     0xFF    0xFF   0xFF     0xFF    0xFF    0xFF    0xFF
    * 
    * client1/building1/ + /sensor1 (empty) (empty) (empty) (empty)
    * \_____/ \_______/ \_/ \_____/ \_____/ \_____/ \_____/ \_____/
    *    |        |      |     |       |       |       |       |
    *   0xFF     0xFF    0    0xFF    0xFF    0xFF    0xFF    0xFF
    * 
    * client1/ + /level1/ #  (empty) (empty) (empty) (empty)
    * \_____/ \_/ \____/ \_/ \_____/ \_____/ \_____/ \_____/
    *    |     |    |     |     |       |       |       |
    *   0xFF   0    0xFF  0     0       0       0       0
    * 
    * 
    * Topic hash and hash mask properties
    * -----------------------------------
    * The following properties of topic hashes and hash masks can be exploited to
    * find potentially matching subscribed topics for a given published topic.
    * 
    * (1) If a subscribed topic does not contain wildcards then the topic hash of the
    * subscribed topic must be equal to the topic hash of the published topic,
    * otherwise the subscribed topic cannot be a candidate for a match.
    * 
    * (2) If a subscribed topic contains wildcards then the hash of the published
    * topic masked with the subscribed topic's hash mask must be equal to the hash of
    * the subscribed topic. I.e. a subscribed topic is a candidate for a match if:
    * (publishedTopicHash & subscribedTopicHashMask) == subscribedTopicHash
    * 
    * (3) If a subscribed topic contains wildcards then any potentially matching
    * published topic must have a hash value that is greater than or equal to the
    * hash value of the subscribed topic (because the subscribed topic contains
    * zeroes in wildcard positions).
    * 
    * Match finding
    * -------------
    * The subscription manager maintains two separate dictionaries to assist finding
    * matches using topic hashes: a _noWildcardSubscriptionsByTopicHash dictionary
    * containing all subscriptions that do not have wildcards, and a
    * _wildcardSubscriptionsByTopicHash dictionary containing subscriptions with
    * wildcards.
    * 
    * For subscriptions without wildcards, all potential candidates for a match are
    * obtained by a single look-up (exploiting point 1 above).
    * 
    * For subscriptions with wildcards, the subscription manager loops through the
    * wildcard subscriptions and selects candidates that satisfy condition 
    * (publishedTopicHash & subscribedTopicMask) == subscribedTopicHash (point 2).
    * The loop could exit early if wildcard subscriptions were stored into a sorted
    * dictionary (utilizing point 3), but, after testing, there does not seem to be
    * any real benefit doing so.
    * 
    * Other considerations
    * --------------------
    * Characters in the topic string are cast to byte and any additional bytes in a
    * multi-byte character are disregarded. Best guess is that this does not impact
    * performance in practice.
    * 
    * Instead of one-byte checksums per topic level, one-word checksums per topic
    * level could be used. If most topics contained four levels or less then hash
    * buckets would be shallower.
    * 
    * For very large numbers of topics, performing a parallel search may help further.
    * 
    * To also handle a larger number of subscribers, it may be beneficial to maintain
    * a subscribers-by-subscription-topic dictionary.
    */
    public static class MqttTopicHash
    {
        public static void Calculate(string topic, out ulong resultHash, out ulong resultHashMask, out bool resultHasWildcard)
        {
            // calculate topic hash
            ulong hash = 0;
            ulong hashMaskInverted = 0;
            ulong levelBitMask = 0;
            ulong fillLevelBitMask = 0;
            var hasWildcard = false;
            byte checkSum = 0;
            var level = 0;

            var i = 0;
            while (i < topic.Length)
            {
                var c = topic[i];
                if (c == MqttTopicFilterComparer.LevelSeparator)
                {
                    // done with this level
                    hash <<= 8;
                    hash |= checkSum;
                    hashMaskInverted <<= 8;
                    hashMaskInverted |= levelBitMask;
                    checkSum = 0;
                    levelBitMask = 0;
                    ++level;
                    if (level >= 8)
                    {
                        break;
                    }
                }
                else if (c == MqttTopicFilterComparer.SingleLevelWildcard)
                {
                    levelBitMask = 0xff;
                    hasWildcard = true;
                }
                else if (c == MqttTopicFilterComparer.MultiLevelWildcard)
                {
                    // checksum is zero for a valid topic
                    levelBitMask = 0xff;
                    // fill rest with this fillLevelBitMask
                    fillLevelBitMask = 0xff;
                    hasWildcard = true;
                    break;
                }
                else
                {
                    // The checksum should be designed to reduce the hash bucket depth for the expected
                    // fairly regularly named MQTT topics that don't differ much,
                    // i.e. "room1/sensor1"
                    //      "room1/sensor2"
                    //      "room1/sensor3"
                    // etc.
                    if ((c & 1) == 0)
                    {
                        checkSum += (byte)c;
                    }
                    else
                    {
                        checkSum ^= (byte)(c >> 1);
                    }
                }

                ++i;
            }

            // Shift hash left and leave zeroes to fill ulong
            if (level < 8)
            {
                hash <<= 8;
                hash |= checkSum;
                hashMaskInverted <<= 8;
                hashMaskInverted |= levelBitMask;
                ++level;
                while (level < 8)
                {
                    hash <<= 8;
                    hashMaskInverted <<= 8;
                    hashMaskInverted |= fillLevelBitMask;
                    ++level;
                }
            }

            if (!hasWildcard)
            {
                while (i < topic.Length)
                {
                    var c = topic[i];
                    if (c == MqttTopicFilterComparer.SingleLevelWildcard || c == MqttTopicFilterComparer.MultiLevelWildcard)
                    {
                        hasWildcard = true;
                        break;
                    }

                    ++i;
                }
            }

            resultHash = hash;
            resultHashMask = ~hashMaskInverted;
            resultHasWildcard = hasWildcard;
        }
    }
}