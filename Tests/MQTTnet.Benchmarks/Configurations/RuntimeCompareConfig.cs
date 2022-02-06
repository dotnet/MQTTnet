// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace MQTTnet.Benchmarks.Configurations
{
    public class RuntimeCompareConfig : BaseConfig
    {
        public RuntimeCompareConfig()
        {
            AddJob(Job.Default.WithRuntime(ClrRuntime.Net472));
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core22).WithToolchain(CsProjCoreToolchain.NetCoreApp22));
        }

    }
}
