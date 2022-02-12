// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;

namespace MQTTnet.Formatter
{
    public sealed class MqttPacketBuffer
    {
        readonly ArraySegment<byte> _segment0;
        readonly ArraySegment<byte> _segment1;

        public MqttPacketBuffer(ArraySegment<byte> segment0, ArraySegment<byte> segment1 = default)
        {
            _segment0 = segment0;
            _segment1 = segment1;

            Length = segment0.Count + segment1.Count;
        }

        public int Length { get; }

        public ArraySegment<byte> ToArray()
        {
            if (_segment1.Count == 0)
            {
                return _segment0;
            }

            var buffer = new byte[Length];
            Array.Copy(_segment0.Array, _segment0.Offset, buffer, 0, _segment0.Count);
            Array.Copy(_segment1.Array, _segment1.Offset, buffer, _segment0.Count, _segment1.Count);

            return new ArraySegment<byte>(buffer);
        }

        public async Task WriteToAsync(IMqttChannel channel, CancellationToken cancellationToken)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            await channel.WriteAsync(_segment0.Array, _segment0.Offset, _segment0.Count, cancellationToken).ConfigureAwait(false);

            if (_segment1.Count > 0)
            {
                await channel.WriteAsync(_segment1.Array, _segment1.Offset, _segment1.Count, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}