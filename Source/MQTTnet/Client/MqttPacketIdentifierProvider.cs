// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttPacketIdentifierProvider
    {
        readonly object _syncRoot = new object();

        ushort _value;

        public void Reset()
        {
            lock (_syncRoot)
            {
                _value = 0;
            }
        }

        public ushort GetNextPacketIdentifier()
        {
            lock (_syncRoot)
            {
                _value++;

                if (_value == 0)
                {
                    // As per official MQTT documentation the package identifier should never be 0.
                    _value = 1;
                }

                return _value;
            }
        }
    }
}
