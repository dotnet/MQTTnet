using System;
using System.Linq;

namespace MQTTnet.Extensions
{
    public static class UserPropertyExtension
    {
        public static string GetUserProperty(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            return message.UserProperties?.SingleOrDefault(up => up.Name.Equals(propertyName, comparisonType))?.Value;
        }
    }
}
