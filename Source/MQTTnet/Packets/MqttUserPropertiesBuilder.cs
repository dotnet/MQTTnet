// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUserPropertiesBuilder
    {
        readonly List<MqttUserProperty> _properties = new List<MqttUserProperty>();

        public List<MqttUserProperty> Build()
        {
            return _properties;
        }

        public MqttUserPropertiesBuilder With(string name, string value = "")
        {
            _properties.Add(new MqttUserProperty(name, value));
            return this;
        }
    }
}