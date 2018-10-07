using System.IO.Pipelines;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class DuplexPipeMockup : IDuplexPipe
    {
        PipeReader IDuplexPipe.Input => Receive.Reader;

        PipeWriter IDuplexPipe.Output => Send.Writer;


        public Pipe Receive { get; set; } = new Pipe();

        public Pipe Send { get; set; } = new Pipe();
    }
}
