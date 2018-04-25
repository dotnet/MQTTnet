using System;

namespace MQTTnet.Server
{
    public static class MqttTopicFilterComparer
    {
        private static readonly char[] TopicLevelSeparator = { '/' };

        public static bool IsMatch(string topic, string sub)
        {
            if (string.IsNullOrEmpty(topic)) throw new ArgumentNullException(nameof(topic));
            if (string.IsNullOrEmpty(sub)) throw new ArgumentNullException(nameof(sub));


            var spos = 0;
            var slen = sub.Length;
            var tpos = 0;
            var tlen = topic.Length;
                        
            var multilevel_wildcard = false;

            while (spos < slen && tpos <= tlen)
            {
                if (sub[spos] == topic[tpos])
                {
                    if (tpos == tlen - 1)
                    {
                        /* Check for e.g. foo matching foo/# */
                        if (spos == slen - 3
                                && sub[spos + 1] == '/'
                                && sub[spos + 2] == '#')
                        {
                            multilevel_wildcard = true;
                            return true;
                        }
                    }
                    spos++;
                    tpos++;
                    if (spos == slen && tpos == tlen)
                    {
                        return true;
                    }
                    else if (tpos == tlen && spos == slen - 1 && sub[spos] == '+')
                    {
                        if (spos > 0 && sub[spos - 1] != '/')
                        {
                            throw new ArgumentException("Invalid filter string", "filter");
                        }
                        spos++;
                        return true;
                    }
                }
                else
                {
                    if (sub[spos] == '+')
                    {
                        /* Check for bad "+foo" or "a/+foo" subscription */
                        if (spos > 0 && sub[spos - 1] != '/')
                        {
                            throw new ArgumentException("Invalid filter string", "filter");
                        }
                        /* Check for bad "foo+" or "foo+/a" subscription */
                        if (spos < slen - 1 && sub[spos + 1] != '/')
                        {
                            throw new ArgumentException("Invalid filter string", "filter");
                        }
                        spos++;
                        while (tpos < tlen && topic[tpos] != '/')
                        {
                            tpos++;
                        }
                        if (tpos == tlen && spos == slen)
                        {
                            return true;
                        }
                    }
                    else if (sub[spos] == '#')
                    {
                        if (spos > 0 && sub[spos - 1] != '/')
                        {
                            throw new ArgumentException("Invalid filter string", "filter");
                        }
                        multilevel_wildcard = true;
                        if (spos + 1 != slen)
                        {
                            throw new ArgumentException("Invalid filter string", "filter");
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
                                && sub[spos - 1] == '+'
                                && sub[spos] == '/'
                                && sub[spos + 1] == '#')
                        {
                            multilevel_wildcard = true;
                            return true;
                        }
                        return false;
                    }
                }
            }
            if (multilevel_wildcard == false && (tpos < tlen || spos < slen))
            {
                return false;
            }

            return false;

         
        }
    }
}
