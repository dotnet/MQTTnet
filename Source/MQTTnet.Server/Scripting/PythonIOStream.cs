using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace MQTTnet.Server.Scripting
{
    public class PythonIOStream : Stream
    {
        readonly ILogger _logger;
        readonly Encoding _encoder = Encoding.UTF8;

        public PythonIOStream(ILogger<PythonIOStream> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            if (count == 0)
            {
                return;
            }

            var text = _encoder.GetString(buffer, offset, count);
            if (text.Equals(Environment.NewLine))
            {
                return;
            }

            _logger.LogDebug(text);
        }

        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get; } = 0L;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
