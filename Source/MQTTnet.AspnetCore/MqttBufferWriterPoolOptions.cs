// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using System;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttBufferWriterPoolOptions
    {
        /// <summary>
        /// When the lifetime of the <see cref="MqttBufferWriter"/> is less than this value, <see cref="MqttBufferWriter"/> is pooled.
        /// </summary>
        public TimeSpan MaxLifetime { get; set; } = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// Whether to pool <see cref="MqttBufferWriter"/> with BufferSize greater than the default buffer size.
        /// </summary>
        public bool LargeBufferSizeEnabled { get; set; } = true;
    }
}
