// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public static class CompletedTask
    {
#if NET452
        public static Task Instance => Task.FromResult(0);
#else
        public static Task Instance => Task.CompletedTask;
#endif
    }
}