// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipelines;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class DuplexPipeMockup : IDuplexPipe
    {
        public DuplexPipeMockup()
        {
            var pool = new LimitedMemoryPool();
            var pipeOptions = new PipeOptions(pool);
            Receive = new Pipe(pipeOptions);
            Send = new Pipe(pipeOptions);
        }

        PipeReader IDuplexPipe.Input => Receive.Reader;

        PipeWriter IDuplexPipe.Output => Send.Writer;


        public Pipe Receive { get; set; } 

        public Pipe Send { get; set; }
    }
}
