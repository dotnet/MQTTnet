// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketBuffer
    {
        readonly ArraySegment<byte>[] _segments;

        public MqttPacketBuffer(params ArraySegment<byte>[] segments)
        {
            _segments = segments ?? throw new ArgumentNullException(nameof(segments));

            Length = _segments.Sum(s => s.Count);
        }

        public int Length { get; }
        
        public ArraySegment<byte> ToArray()
        {
            if (_segments.Length == 1)
            {
                return _segments[0];
            }
            
            using (var buffer = new MemoryStream(Length))
            {
                foreach (var segment in _segments)
                {
                    buffer.Write(segment.Array, segment.Offset, segment.Count);
                }

                return new ArraySegment<byte>(buffer.ToArray());    
            }
        }

        public async Task WriteToAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            
            foreach (var segment in _segments)
            {
                await channel.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}