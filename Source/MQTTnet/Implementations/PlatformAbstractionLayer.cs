// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public static class PlatformAbstractionLayer
    {
#if NET452
        public static Task CompletedTask => Task.FromResult(0);

        public static byte[] EmptyByteArray { get; } = new byte[0];
#else
        public static Task CompletedTask => Task.CompletedTask;

        public static byte[] EmptyByteArray { get; } = Array.Empty<byte>();
#endif

        public static void Sleep(TimeSpan timeout)
        {
#if !NETSTANDARD1_3 && !WINDOWS_UWP
            try
            {
                System.Threading.Thread.Sleep(timeout);
            }
            catch (ThreadAbortException)
            {
                // The ThreadAbortException is not actively catched in this project.
                // So we use a one which is similar and will be catched properly.
                throw new OperationCanceledException();
            }
#else
            Task.Delay(timeout).Wait();
#endif
        }
    }
}
