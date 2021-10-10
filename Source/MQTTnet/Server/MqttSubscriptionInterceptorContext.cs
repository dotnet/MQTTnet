using System;
using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttSubscriptionInterceptorContext
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets or sets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; internal set; }

        // Will be removed together with "AcceptSubscription". It only stores the default value when setting "AcceptSubscription" to true.
        internal MqttSubscribeReasonCode DefaultReasonCode { get; set; }
        
        internal MqttSubscribeReturnCode DefaultReturnCode { get; set; }

        [Obsolete("Please use a proper value for 'ReasonCode' instead. This property will be removed in the future.")]
        public bool AcceptSubscription
        {
            get => ReasonCode == MqttSubscribeReasonCode.GrantedQoS0 && ReasonCode <= MqttSubscribeReasonCode.GrantedQoS2;
            set
            {
                if (value)
                {
                    ReasonCode = DefaultReasonCode;
                    ReturnCode = DefaultReturnCode;
                }
                else
                {
                    ReturnCode = MqttSubscribeReturnCode.Failure;
                    ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the broker should create an internal subscription for the client.
        /// The broker can also avoid this and return "success" to the client.
        /// This feature allows using the MQTT Broker as the Frontend and another system as the backend.
        /// </summary>
        public bool ProcessSubscription { get; set; } = true;

        /// <summary>
        /// Gets or sets the reason code which is sent to the client.
        /// The subscription is skipped when the value is not GrantedQoS_.
        /// MQTTv5 only.
        /// </summary>
        public MqttSubscribeReasonCode ReasonCode { get; set; }

        // MQTT < 5 only!
        internal MqttSubscribeReturnCode ReturnCode { get; set; }

        /// <summary>
        /// Gets or sets whether the broker should close the client connection.
        /// </summary>
        public bool CloseConnection { get; set; }
    }
}