using System;
using System.Text;

namespace MQTTnet
{
    public static class MqttApplicationMessageExtensions
    {
        /// <summary>
        /// Converts the payload to a string if possible.
        /// </summary>
        /// <param name="applicationMessage">The application message.</param>
        /// <returns>A string.</returns>
        public static string ConvertPayloadToString(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            if (applicationMessage.Payload == null)
            {
                return null;
            }

            if (applicationMessage.Payload.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(applicationMessage.Payload, 0, applicationMessage.Payload.Length);
        }
    }
}
