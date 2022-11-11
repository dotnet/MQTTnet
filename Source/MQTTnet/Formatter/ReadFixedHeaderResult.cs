// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Formatter
{
    public struct ReadFixedHeaderResult
    {
        public static ReadFixedHeaderResult Canceled { get; } = new ReadFixedHeaderResult
        {
            IsCanceled = true
        };
        
        public static ReadFixedHeaderResult ConnectionClosed { get; } = new ReadFixedHeaderResult
        {
            IsConnectionClosed = true
        };
        
        public bool IsCanceled { get; set; }
        
        public bool IsConnectionClosed { get; set; }

        public MqttFixedHeader FixedHeader { get; set; }
    }
}
