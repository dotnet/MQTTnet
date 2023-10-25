// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MQTTnet.Internal;

namespace MQTTnet
{
    public static class MqttApplicationMessageExtensions
    {
        public static string ConvertPayloadToString(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if(applicationMessage.PayloadSegment == EmptyBuffer.ArraySegment)
            {
                return null;
            }

            if (applicationMessage.PayloadSegment.Array == null)
            {
                return null;
            }

            var payloadSegment = applicationMessage.PayloadSegment;
            return Encoding.UTF8.GetString(payloadSegment.Array, payloadSegment.Offset, payloadSegment.Count);
        }
    }
}
