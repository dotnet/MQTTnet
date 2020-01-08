using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttSubscriptionInterceptorContext
    {
        private bool _acceptSubscription = true;
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
            get => _acceptSubscription;
            set {
                if (!value && _resultCode < MqttSubscribeReasonCode.UnspecifiedError)
                {
                    _resultCode = MqttSubscribeReasonCode.UnspecifiedError;
                }
                _acceptSubscription = value;
            }
        }

        public MqttSubscribeReasonCode ResultCode
        {
            get => _resultCode;
            set {
                if (AcceptSubscription && value >= MqttSubscribeReasonCode.UnspecifiedError)
                {
                    AcceptSubscription = false;
                }
                _resultCode = value;
            }
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
