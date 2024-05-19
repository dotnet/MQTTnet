// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Diagnostics;

public static class TargetFrameworkProvider
{
    public static string TargetFramework
    {
        get
        {
#if NET6_0
                return "net6.0";
#elif NET8_0
            return "net8.0";
#endif
        }
    }
}