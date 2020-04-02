using System;
using System.Linq;

namespace MQTTnet.Extensions
{
    public static class UserPropertyExtension
    {
        public static string GetUserProperty(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            return message.UserProperties?.SingleOrDefault(up => up.Name.Equals(propertyName, comparisonType))?.Value;
        }

        public static T GetUserProperty<T>(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var value = GetUserProperty(message, propertyName, comparisonType);

            try
            {
                return (T) Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert value({value}) of UserProperty({propertyName}) to {typeof(T).FullName}.", ex);
            }
        }
    }
}
