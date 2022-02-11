// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttSubscription
    {
        public MqttSubscription(
           string topic,
           bool noLocal,
           MqttRetainHandling retainHandling,
           bool retainAsPublished,
           MqttQualityOfServiceLevel qualityOfServiceLevel,
           uint identifier
       )
        {
            Topic = topic;
            NoLocal = noLocal;
            RetainHandling = retainHandling;
            RetainAsPublished = retainAsPublished;
            GrantedQualityOfServiceLevel = qualityOfServiceLevel;
            Identifier = identifier;
            // calculate topic hash
            ulong hash;
            ulong hashMask;
            bool hasWildcard;
            CalcTopicHash(Topic, out hash, out hashMask, out hasWildcard);
            TopicHash = hash;
            TopicHashMask = hashMask;
            TopicHasWildcard = hasWildcard;
        }

        public string Topic { get; private set; }

        public bool NoLocal { get; private set; }

        public MqttRetainHandling RetainHandling { get; private set; }

        public bool RetainAsPublished { get; private set; }

        public MqttQualityOfServiceLevel GrantedQualityOfServiceLevel { get; private set; }

        public uint Identifier { get; private set; }

        public ulong TopicHash { get; private set; }

        public ulong TopicHashMask { get; private set; }

        public bool TopicHasWildcard { get; private set; }

        public static void CalcTopicHash(string topic, out ulong resultHash, out ulong resultHashMask, out bool resultHasWildcard)
        {
            // calculate topic hash
            ulong hash = 0;
            ulong hashMaskInverted = 0;
            ulong levelBitMask = 0;
            ulong fillLevelBitMask = 0;
            var hasWildcard = false;
            byte checkSum = 0;
            int level = 0;

            int i = 0;
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
                        break;
                }
                else if (c == MqttTopicFilterComparer.SingleLevelWildcard)
                {
                    levelBitMask = (byte)0xff;
                    hasWildcard = true;
                }
                else if (c == MqttTopicFilterComparer.MultiLevelWildcard)
                {
                    // checksum is zero for a valid topic
                    levelBitMask = (byte)0xff;
                    // fill rest with this fillLevelBitMask
                    fillLevelBitMask = (byte)0xff;
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
                    if ((c == MqttTopicFilterComparer.SingleLevelWildcard) || (c == MqttTopicFilterComparer.MultiLevelWildcard))
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