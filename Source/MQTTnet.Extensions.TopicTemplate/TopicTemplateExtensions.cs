using System;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.TopicTemplate
{
    public static class TopicTemplateExtensions
    {

        /// <summary>
        /// Set the filter topic according to the template, with
        /// template parameters substituted by a single-level
        /// wildcard.
        /// </summary>
        /// <param name="builder">a filter builder</param>
        /// <param name="topicTemplate">a topic template</param>
        /// <param name="subscribeTreeRoot">whether to subscribe to the whole topic tree</param>
        /// <returns>the modified topic filter</returns>
        public static MqttTopicFilterBuilder WithTopicTemplate(
            this MqttTopicFilterBuilder builder,
            MqttTopicTemplate topicTemplate,
            bool subscribeTreeRoot = false)
        {
            return builder.WithTopic(
                subscribeTreeRoot ? 
                    topicTemplate.TopicTreeRootFilter :
                    topicTemplate.TopicFilter);
        }

        /// <summary>
        /// Set the publication topic according to the topic template. The template
        /// must not have remaining (unset) parameters or contain wildcards.
        /// </summary>
        /// <param name="builder">a message builder</param>
        /// <param name="topicTemplate">a parameterless topic template</param>
        /// <returns>the modified message builder</returns>
        /// <exception cref="ArgumentException">if the topic template has parameters</exception>
        public static MqttApplicationMessageBuilder WithTopicTemplate(
            this MqttApplicationMessageBuilder builder,
            MqttTopicTemplate topicTemplate)
        {
            if (topicTemplate.Parameters.Any())
                throw new ArgumentException("topic templates must be parameter-less when sending " + topicTemplate.Template);
            MqttTopicValidator.ThrowIfInvalid(topicTemplate.Template);
            return builder.WithTopic(topicTemplate.Template);
        }
        
        /// <summary>
        /// Set the filter topic according to the template, with
        /// remaining template parameters substituted by single-level
        /// wildcard.
        /// </summary>
        /// <param name="builder">a filter builder</param>
        /// <param name="topicTemplate">a topic template</param>
        /// <param name="subscribeTreeRoot">whether to subscribe to the whole topic tree</param>
        /// <returns>the modified topic filter</returns>
        public static MqttTopicFilterBuilder BuildFilter(this MqttTopicTemplate topicTemplate, bool subscribeTreeRoot = false)
            => new MqttTopicFilterBuilder().WithTopicTemplate(topicTemplate, subscribeTreeRoot);

        /// <summary>
        /// Return whether the message matches the given topic template.
        /// </summary>
        /// <param name="message">a message</param>
        /// <param name="topicTemplate">a topic template</param>
        /// <param name="subtree">whether to include the topic subtree</param>
        /// <returns></returns>
        public static bool MatchesTopicTemplate(
            this MqttApplicationMessage message,
            MqttTopicTemplate topicTemplate,
            bool subtree = false)
            => topicTemplate.MatchesTopic(message.Topic, subtree);

        /// <summary>
        /// Return a message builder to respond to this message. The
        /// message's response topic and correlation data are included
        /// in the response message builder.
        /// </summary>
        /// <param name="message">a message with a response topic</param>
        /// <returns>a message builder</returns>
        /// <see cref="AsResponseTo"/>
        public static MqttApplicationMessageBuilder BuildResponse(
            this MqttApplicationMessage message)
            => new MqttApplicationMessageBuilder().AsResponseTo(message);

        /// <summary>
        /// Modify this message builder to respond to a given message. The
        /// message's response topic and correlation data are included
        /// in the message builder.
        /// </summary>
        /// <param name="builder">a message builder</param>
        /// <param name="message">a message with a response topic</param>
        /// <returns>a message builder</returns>
        /// <see cref="BuildResponse"/>
        public static MqttApplicationMessageBuilder AsResponseTo(
            this MqttApplicationMessageBuilder builder,
            MqttApplicationMessage message)
        {
            if (!string.IsNullOrEmpty(message.ResponseTopic))
                throw new ArgumentException("message does not have a response topic");
            return builder
                .WithTopic(message.ResponseTopic)
                .WithCorrelationData(message.CorrelationData);
        }

    }
}
