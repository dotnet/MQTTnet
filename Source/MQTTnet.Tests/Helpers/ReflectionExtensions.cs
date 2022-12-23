// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace MQTTnet.Tests.Helpers
{
    public static class ReflectionExtensions
    {
        public static object GetFieldValue(this object source, string fieldName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var field = source.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new ArgumentException($"Field {fieldName} not found.");
            }
            
            return field.GetValue(source);
        }
    }
}