using System;
using System.Globalization;
using System.IO;
using System.Text;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core
{
    public class MqttApplicationMessageFactory
    {
        public MqttApplicationMessage CreateApplicationMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            if (payload == null)
            {
                payload = new byte[0];
            }

            return new MqttApplicationMessage(topic, payload, qualityOfServiceLevel, retain);
        }

        public MqttApplicationMessage CreateApplicationMessage<TPayload>(string topic, TPayload payload, MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var payloadString = Convert.ToString(payload, CultureInfo.InvariantCulture);
            var payloadBuffer = string.IsNullOrEmpty(payloadString) ? new byte[0] : Encoding.UTF8.GetBytes(payloadString);

            return CreateApplicationMessage(topic, payloadBuffer, qualityOfServiceLevel, retain);
        }

        public MqttApplicationMessage CreateApplicationMessage(string topic, Stream payload, MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            byte[] payloadBuffer;
            if (payload == null || payload.Length == 0)
            {
                payloadBuffer = new byte[0];
            }
            else
            {
                payloadBuffer = new byte[payload.Length - payload.Position];
                payload.Read(payloadBuffer, 0, payloadBuffer.Length);
            }

            return CreateApplicationMessage(topic, payloadBuffer, qualityOfServiceLevel, retain);
        }
    }
}
