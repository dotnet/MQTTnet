using System;

namespace MQTTnet.Server
{
    public static class MqttTopicFilterComparer
    {
        private const char LEVEL_SEPARATOR = '/';
        private const char WILDCARD_MULTI_LEVEL = '#';
        private const char WILDCARD_SINGLE_LEVEL = '+';

        public static bool IsMatch(string topic, string filter)
        {
            if (string.IsNullOrEmpty(topic)) throw new ArgumentNullException(nameof(topic));
            if (string.IsNullOrEmpty(filter)) throw new ArgumentNullException(nameof(filter));

            int spos = 0;
            int slen = filter.Length;
            int tpos = 0;
            int tlen = topic.Length;

            while (spos < slen && tpos < tlen)
            {
                if (filter[spos] == topic[tpos])
                {
                    if (tpos == tlen - 1)
                    {
                        /* Check for e.g. foo matching foo/# */
                        if (spos == slen - 3
                                && filter[spos + 1] == LEVEL_SEPARATOR
                                && filter[spos + 2] == WILDCARD_MULTI_LEVEL)
                        {
                            return true;
                        }
                    }
                    spos++;
                    tpos++;
                    if (spos == slen && tpos == tlen)
                    {
                        return true;
                    }
                    else if (tpos == tlen && spos == slen - 1 && filter[spos] == WILDCARD_SINGLE_LEVEL)
                    {
                        if (spos > 0 && filter[spos - 1] != LEVEL_SEPARATOR)
                        {
                            // Invalid filter string
                            return false;
                        }
                        spos++;
                        return true;
                    }
                }
                else
                {
                    if (filter[spos] == WILDCARD_SINGLE_LEVEL)
                    {
                        /* Check for bad "+foo" or "a/+foo" subscription */
                        if (spos > 0 && filter[spos - 1] != LEVEL_SEPARATOR)
                        {
                            // Invalid filter string
                            return false;
                        }
                        /* Check for bad "foo+" or "foo+/a" subscription */
                        if (spos < slen - 1 && filter[spos + 1] != LEVEL_SEPARATOR)
                        {
                            // Invalid filter string
                            return false;
                        }
                        spos++;
                        while (tpos < tlen && topic[tpos] != LEVEL_SEPARATOR)
                        {
                            tpos++;
                        }
                        if (tpos == tlen && spos == slen)
                        {
                            return true;
                        }
                    }
                    else if (filter[spos] == WILDCARD_MULTI_LEVEL)
                    {
                        if (spos > 0 && filter[spos - 1] != LEVEL_SEPARATOR)
                        {
                            // Invalid filter string
                            return false;
                        }
                        if (spos + 1 != slen)
                        {
                            // Invalid filter string
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        /* Check for e.g. foo/bar matching foo/+/# */
                        if (spos > 0
                                && spos + 2 == slen
                                && tpos == tlen
                                && filter[spos - 1] == WILDCARD_SINGLE_LEVEL
                                && filter[spos] == LEVEL_SEPARATOR
                                && filter[spos + 1] == WILDCARD_MULTI_LEVEL)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }
            if (tpos < tlen || spos < slen)
            {
                return false;
            }

            return false;
        }
    }
}
