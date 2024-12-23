// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Rpc
{
    public interface IMqttRpcClient : IDisposable
    {       
        Task<ReadOnlySequence<byte>> ExecuteAsync(string methodName, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, IDictionary<string, object> parameters = null, CancellationToken cancellationToken = default);
    }
}