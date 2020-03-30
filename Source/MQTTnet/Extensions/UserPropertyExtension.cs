using System;
using System.Linq;

namespace MQTTnet.Extensions
{
    public static class UserPropertyExtension
    {
        public static string GetUserProperty(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return message.UserProperties.SingleOrDefault(up => up.Name.Equals(propertyName, comparisonType))?.Value;
        }

        public static T GetUserProperty<T>(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return (T) Convert.ChangeType(GetUserProperty(message, propertyName, comparisonType), typeof(T));
        }
    }
}
