// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global

namespace MQTTnet
{
    public static class MqttApplicationMessageExtensions
    {
        public static short ConvertPayloadToByte(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.Payload.Length != 1)
            {
                throw new ArgumentException("Payload requires to be 1 bytes long.");
            }

            return applicationMessage.Payload[0];
        }

        public static short ConvertPayloadToInt16(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.Payload.Length != 2)
            {
                throw new ArgumentException("Payload requires to be 2 bytes long.");
            }

            return BitConverter.ToInt16(applicationMessage.Payload, 0);
        }

        public static int ConvertPayloadToInt32(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.Payload?.Length != 4)
            {
                throw new ArgumentException("Payload requires to be 4 bytes long.");
            }

            return BitConverter.ToInt32(applicationMessage.Payload, 0);
        }

        public static long ConvertPayloadToInt64(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.Payload.Length != 8)
            {
                throw new ArgumentException("Payload requires to be 8 bytes long.");
            }

            return BitConverter.ToInt64(applicationMessage.Payload, 0);
        }

        public static string ConvertPayloadToString(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (applicationMessage.Payload == null)
            {
                return null;
            }

            if (applicationMessage.Payload.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(applicationMessage.Payload, 0, applicationMessage.Payload.Length);
        }

        public static decimal ParsePayloadToDecimal(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return decimal.Parse(ConvertPayloadToString(applicationMessage));
        }

        public static double ParsePayloadToDouble(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return double.Parse(ConvertPayloadToString(applicationMessage));
        }

        public static short ParsePayloadToInt16(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return short.Parse(ConvertPayloadToString(applicationMessage));
        }

        public static int ParsePayloadToInt32(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return int.Parse(ConvertPayloadToString(applicationMessage));
        }

        public static long ParsePayloadToInt64(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return long.Parse(ConvertPayloadToString(applicationMessage));
        }

        public static float ParsePayloadToSingle(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            return float.Parse(ConvertPayloadToString(applicationMessage));
        }
    }
}