using System;

namespace MQTTnet.Server
{
    public static class MqttTopicFilterComparer
    {
        private const char LevelSeparator = '/';
        private const char MultiLevelWildcard = '#';
        private const char SingleLevelWildcard = '+';

        public static bool IsMatch(string topic, string filter)
        {
            if (string.IsNullOrEmpty(topic)) throw new ArgumentNullException(nameof(topic));
            if (string.IsNullOrEmpty(filter)) throw new ArgumentNullException(nameof(filter));

            var sPos = 0;
            var sLen = filter.Length;
            var tPos = 0;
            var tLen = topic.Length;

            while (sPos < sLen && tPos < tLen)
            {
                if (filter[sPos] == topic[tPos])
                {
                    if (tPos == tLen - 1)
                    {
                        // Check for e.g. foo matching foo/#
                        if (sPos == sLen - 3
                                && filter[sPos + 1] == LevelSeparator
                                && filter[sPos + 2] == MultiLevelWildcard)
                        {
                            return true;
                        }
                    }

                    sPos++;
                    tPos++;

                    if (sPos == sLen && tPos == tLen)
                    {
                        return true;
                    }

                    if (tPos == tLen && sPos == sLen - 1 && filter[sPos] == SingleLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                }
                else
                {
                    if (filter[sPos] == SingleLevelWildcard)
                    {
                        // Check for bad "+foo" or "a/+foo" subscription
                        if (sPos > 0 && filter[sPos - 1] != LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        // Check for bad "foo+" or "foo+/a" subscription
                        if (sPos < sLen - 1 && filter[sPos + 1] != LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        sPos++;
                        while (tPos < tLen && topic[tPos] != LevelSeparator)
                        {
                            tPos++;
                        }

                        if (tPos == tLen && sPos == sLen)
                        {
                            return true;
                        }
                    }
                    else if (filter[sPos] == MultiLevelWildcard)
                    {
                        if (sPos > 0 && filter[sPos - 1] != LevelSeparator)
                        {
                            // Invalid filter string
                            return false;
                        }

                        if (sPos + 1 != sLen)
                        {
                            // Invalid filter string
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        // Check for e.g. foo/bar matching foo/+/#
                        if (sPos > 0
                                && sPos + 2 == sLen
                                && tPos == tLen
                                && filter[sPos - 1] == SingleLevelWildcard
                                && filter[sPos] == LevelSeparator
                                && filter[sPos + 1] == MultiLevelWildcard)
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
        }
    }
}
