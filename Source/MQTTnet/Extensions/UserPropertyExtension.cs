// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
