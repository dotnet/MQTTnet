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
