// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed partial class ClientConnectionContext : ConnectionContext
    {
        private readonly Stream _stream;
        private readonly CancellationTokenSource _connectionCloseSource = new();

        private IDictionary<object, object?>? _items;

        public override IDuplexPipe Transport { get; set; }

        public override CancellationToken ConnectionClosed
        {
            get => _connectionCloseSource.Token;
            set => throw new InvalidOperationException();
        }

        public override string ConnectionId { get; set; } = string.Empty;

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override IDictionary<object, object?> Items
        {
            get => _items ??= new Dictionary<object, object?>();
            set => _items = value;
        }

        public ClientConnectionContext(Stream stream)
        {
            _stream = stream;
            Transport = new StreamTransport(stream);
        }

        public override async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            _connectionCloseSource.Cancel();
            _connectionCloseSource.Dispose();
        }

        public override void Abort()
        {
            _stream.Close();
            _connectionCloseSource.Cancel();
        }


        private class StreamTransport(Stream stream) : IDuplexPipe
        {
            public PipeReader Input { get; } = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));

            public PipeWriter Output { get; } = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }
    }
}
