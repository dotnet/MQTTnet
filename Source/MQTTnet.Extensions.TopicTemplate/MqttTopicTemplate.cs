using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.TopicTemplate
{
    /// <summary>
    /// A topic template is an MQTT topic filter string that may contain
    /// segments in curly braces called parameters. This well-known
    /// 'moustache' syntax also matches AsyncAPI Channel Address Expressions.
    /// The topic template is designed to support dynamic subscription/publication,
    /// message-topic matching and routing. It is intended to be more convenient
    /// than String.Format() for aforementioned purposes.
    /// </summary>
    /// <example>
    /// topic/subtopic/{parameter}/{otherParameter}
    /// </example>
    public sealed class MqttTopicTemplate : IEquatable<MqttTopicTemplate>
    {
        private readonly string topicTemplate;
        private readonly string[] parameterSegments;
        private string topicFilter;

        private static readonly Regex moustacheRegex = new Regex("{([^/]+?)}", RegexOptions.Compiled);

        /// <summary>
        /// Create a topic template from an mqtt topic filter with moustache placeholders.
        /// </summary>
        /// <param name="topicTemplate"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MqttTopicTemplate(string topicTemplate)
        {
            if (topicTemplate == null)
                throw new ArgumentNullException(nameof(topicTemplate));
            MqttTopicValidator.ThrowIfInvalidSubscribe(topicTemplate);

            this.topicTemplate = topicTemplate;
            this.parameterSegments = topicTemplate.Split(MqttTopicFilterComparer.LevelSeparator)
                .Select(segment => moustacheRegex.Match(segment).Groups[1].Value)
                .Select(s => s.Length > 0 ? s : null)
                .ToArray();
        }

        /// <summary>
        /// Try to set a parameter to a given value. If the parameter is not present,
        /// this is returned. The value must not contain slashes.
        /// </summary>
        /// <param name="parameter">a template parameter</param>
        /// <param name="value">a string</param>
        /// <returns></returns>
        public MqttTopicTemplate TrySetParameter(string parameter, string value)
        {
            if (parameter != null && parameterSegments.Contains(parameter))
            {
                return WithParameter(parameter, value);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Substitute a parameter with a given value, thus removing the parameter. If the parameter is not present,
        /// the method trows. The value must not contain slashes.
        /// </summary>
        /// <param name="parameter">a template parameter</param>
        /// <param name="value">a string</param>
        /// <exception cref="ArgumentException">when the parameter is not present</exception>
        /// <returns>the topic template (without the parameter)</returns>
        public MqttTopicTemplate WithParameter(string parameter, string value)
        {
            if (value == null ||
                string.IsNullOrEmpty(parameter) ||
                !parameterSegments.Contains(parameter) ||
                value.Contains(MqttTopicFilterComparer.LevelSeparator) ||
                value.Contains(MqttTopicFilterComparer.MultiLevelWildcard))
                throw new ArgumentException("parameter must exist and value must not contain slashes.");

            var moustache = "{" + parameter + "}";
            return new MqttTopicTemplate(topicTemplate.Replace(moustache, value));
        }

        /// <summary>
        /// Replace the given parameter with a single-level wildcard (plus sign).
        /// </summary>
        /// <param name="parameter">parameter name</param>
        /// <returns>the topic template (without the parameter)</returns>
        public MqttTopicTemplate WithoutParameter(string parameter) => WithParameter(parameter, MqttTopicFilterComparer.SingleLevelWildcard.ToString());

        /// <summary>
        /// Yield the template parameter names.
        /// </summary>
        public IEnumerable<string> Parameters => parameterSegments.Where(s => s != null);

        /// <summary>
        /// The topic template as an MQTT topic filter (+ substituted for all parameters). If the template
        /// ends with a multi-level wildcard (hash), this will be reflected here.
        /// </summary>
        public string TopicFilter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref topicFilter, 
                    () => moustacheRegex.Replace(topicTemplate, MqttTopicFilterComparer.SingleLevelWildcard.ToString()));
                return topicFilter;
            }
        }

        /// <summary>
        /// Return the topic filter of this template, ending with a multi-level wildcard (hash).
        /// </summary>
        public string TopicTreeRootFilter
        {
            get
            {
                var filter = TopicFilter;
                // append slash if neccessary
                if (filter.Length > 0 && !filter.EndsWith(MqttTopicFilterComparer.LevelSeparator.ToString()) && !filter.EndsWith(MqttTopicFilterComparer.MultiLevelWildcard.ToString()))
                    filter += MqttTopicFilterComparer.LevelSeparator;
                // append hash if neccessary
                if (!filter.EndsWith(MqttTopicFilterComparer.MultiLevelWildcard.ToString()))
                    filter += MqttTopicFilterComparer.MultiLevelWildcard;

                return filter;
            }
        }

        /// <summary>
        /// Test if this topic template matches a given topic.
        /// </summary>
        /// <param name="topic">a fully specified topic</param>
        /// <param name="subtree">true to match including the subtree (multi-level wildcard)</param>
        /// <returns>true iff the topic matches the template's filter</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException">if the topic is invalid</exception>
        public bool MatchesTopic(string topic, bool subtree = false)
        {
            var comparison = MqttTopicFilterComparer.Compare(topic, subtree ? TopicTreeRootFilter : TopicFilter);
            if (comparison == MqttTopicFilterCompareResult.FilterInvalid)
                throw new InvalidOperationException("Invalid filter");
            else if (comparison == MqttTopicFilterCompareResult.TopicInvalid)
                throw new ArgumentException("Invalid topic", nameof(topic));
            else
                return comparison == MqttTopicFilterCompareResult.IsMatch;
        }

        /// <summary>
        /// Extract the parameter values from a topic corresponding to the template
        /// parameters. The topic has to match this template.
        /// </summary>
        /// <param name="topic">the topic</param>
        /// <returns>an enumeration of (parameter, index, value)</returns>
        public IEnumerable<(string parameter, int index, string value)> ParseParameterValues(string topic)
        {
            if (!MatchesTopic(topic))
                throw new ArgumentException("the topic has to match this template", nameof(topic));

            return parseParameterValuesInternal(topic);
        }

        /// <summary>
        /// Extract the parameter values from the message topic corresponding to the template
        /// parameters. The message topic has to match this topic template.
        /// </summary>
        /// <param name="message">the message</param>
        /// <returns>an enumeration of (parameter, index, value)</returns>
        public IEnumerable<(string parameter, int index, string value)> ParseParameterValues(MqttApplicationMessage message)
            => ParseParameterValues(message.Topic);

        private IEnumerable<(string parameter, int index, string value)> parseParameterValuesInternal(string topic)
        {
            // because we have a match, we know the segment array is at least the template's length
            var segments = topic.Split(MqttTopicFilterComparer.LevelSeparator);
            for (int i = 0; i < parameterSegments.Length; i++)
            {
                var name = parameterSegments[i];
                if (name != null)
                    yield return (name, i, segments[i]);
            }
        }

        /// <summary>
        /// Reuse parameters as they are extracted using another topic template on this template
        /// when the parameter name matches. Useful
        /// for compatibility routing.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public MqttTopicTemplate WithParameterValuesFrom(IEnumerable<(string parameter, int index, string value)> parameters)
            => parameters.Aggregate(this, (t, p) => t.TrySetParameter(p.parameter, p.value));

        /// <summary>
        /// The topic template string representation, e.g. A/B/{foo}/D.
        /// </summary>
        public string Template => topicTemplate;

        public bool Equals(MqttTopicTemplate other)
        {
            return other != null && topicTemplate == other.topicTemplate;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((MqttTopicTemplate)obj);
        }

        public override int GetHashCode()
        {
            return topicTemplate.GetHashCode();
        }

        /// <summary>
        /// Determine the shortest common prefix of the given templates. Partial segments
        /// are not returned.
        /// </summary>
        /// <param name="templates">topic templates</param>
        /// <returns></returns>
        public static MqttTopicTemplate FindCanonicalPrefix(IEnumerable<MqttTopicTemplate> templates)
        {
            string root = null;

            string commonPrefix(string a, string b)
            {
                var maxIndex = Math.Min(a.Length, b.Length) - 1;
                for (int i = 0; i <= maxIndex; i++)
                {
                    if (a[i] != b[i])
                        return a.Substring(0, i);
                }

                return a.Substring(0, maxIndex);
            }

            foreach (string topic in from template in templates select template.Template)
            {
                root = root == null ? topic : commonPrefix(root, topic);
            }

            root = root ?? "";

            return new MqttTopicTemplate(root.Substring(0, root.LastIndexOf('/')));
        }
    }
}