// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Internal;

public static class CancellationTokenSourceExtensions
{
    public static bool TryCancel(this CancellationTokenSource cancellationTokenSource, bool throwOnFirstException = false)
    {
        if (cancellationTokenSource == null)
        {
            return false;
        }

        try
        {
            // Checking the _IsCancellationRequested_ here will not throw an
            // "ObjectDisposedException" as the getter of the property "Token" will do!
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            cancellationTokenSource.Cancel(throwOnFirstException);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}