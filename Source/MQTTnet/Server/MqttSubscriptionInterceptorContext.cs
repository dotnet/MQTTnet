using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttSubscriptionInterceptorContext
    {
        private MqttSubscribeReasonCode _resultCode;

        public MqttSubscriptionInterceptorContext(string clientId, TopicFilter topicFilter, IDictionary<object, object> sessionItems)
        {
            ClientId = clientId;
            TopicFilter = topicFilter;
            SessionItems = sessionItems;
            _resultCode = ConvertToSubscribeReasonCode(topicFilter.QualityOfServiceLevel);
        }

        public string ClientId { get; }

        public TopicFilter TopicFilter { get; set; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> SessionItems { get; }

        public bool AcceptSubscription
        {
            get => _resultCode < MqttSubscribeReasonCode.UnspecifiedError;
            /*  [Obsolete("Set error directly with ResultCode")] // Requires language 8.2 to have here. */
            set {
                if (!value && _resultCode < MqttSubscribeReasonCode.UnspecifiedError)
                {
                    _resultCode = MqttSubscribeReasonCode.UnspecifiedError;
                }
                else if (value && _resultCode >= MqttSubscribeReasonCode.UnspecifiedError) 
                {
                    _resultCode = MqttSubscribeReasonCode.GrantedQoS0;
                }
            }
        }

        public MqttSubscribeReasonCode ResultCode
        {
            get => _resultCode;
            set => _resultCode = value;
        }

        public bool CloseConnection { get; set; }

        public static MqttSubscribeReasonCode ConvertToSubscribeReasonCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReasonCode.GrantedQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReasonCode.GrantedQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReasonCode.GrantedQoS2;
                default: return MqttSubscribeReasonCode.UnspecifiedError;
            }
        }
    }
}
