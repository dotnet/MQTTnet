// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Class to create a MemoryStream which uses ArrayPool buffers.
    /// </summary>
    public sealed class ArrayPoolMemoryStream : MemoryStream
    {
        private const int DefaultBufferListSize = 8;
        private int _bufferIndex;
        private ArraySegment<byte> _currentBuffer;
        private int _currentPosition;
        private List<ArraySegment<byte>> _buffers;
        private int _start;
        private int _count;
        private int _bufferSize;
        private int _endOfLastBuffer;
        private bool _externalBuffersReadOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayPoolMemoryStream"/> class.
        /// Creates a writeable stream that rents ArrayPool buffers as necessary.
        /// </summary>
        public ArrayPoolMemoryStream(int bufferSize, int start, int count)
        {
            _buffers = new List<ArraySegment<byte>>(DefaultBufferListSize);
            _bufferSize = bufferSize;
            _start = start;
            _count = count;
            _endOfLastBuffer = 0;
            _externalBuffersReadOnly = false;

            SetCurrentBuffer(0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayPoolMemoryStream"/> class.
        /// Creates a writeable stream that creates buffers as necessary.
        /// </summary>
        public ArrayPoolMemoryStream(int bufferSize)
        {
            _buffers = new List<ArraySegment<byte>>(DefaultBufferListSize);
            _bufferSize = bufferSize;
            _start = 0;
            _count = bufferSize;
            _endOfLastBuffer = 0;
            _externalBuffersReadOnly = false;
        }

        /// <inheritdoc/>
        public override bool CanRead
        {
            get { return _buffers != null; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return _buffers != null; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return _buffers != null && !_externalBuffersReadOnly; }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get { return GetAbsoluteLength(); }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get { return GetAbsolutePosition(); }
            set { Seek(value, SeekOrigin.Begin); }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            // nothing to do.
        }

        /// <summary>
        /// Returns ReadOnlySequence of the buffers stored in the stream.
        /// ReadOnlySequence is only valid as long as the stream is not
        /// disposed and no more data is written.
        /// </summary>
        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            if (_buffers.Count == 0 || _buffers[0].Array == null)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            int endIndex = GetBufferCount(0);
            if (endIndex == 0)
            {
                return ReadOnlySequence<byte>.Empty;
            }

            var firstSegment = new ArrayPoolBufferSegment<byte>(_buffers[0].Array!, _buffers[0].Offset, endIndex);
            var nextSegment = firstSegment;
            for (int ii = 1; ii < _buffers.Count; ii++)
            {
                var buffer = _buffers[ii];
                if (buffer.Array != null && endIndex > 0)
                {
                    endIndex = GetBufferCount(ii);
                    nextSegment = nextSegment.Append(buffer.Array, buffer.Offset, endIndex);
                }
            }

            return new ReadOnlySequence<byte>(firstSegment, 0, nextSegment, endIndex);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            do
            {
                // check for end of stream.
                if (_currentBuffer.Array == null)
                {
                    return -1;
                }

                int bytesLeft = GetBufferCount(_bufferIndex) - _currentPosition;

                // copy the bytes requested.
                if (bytesLeft > 0)
                {
                    return _currentBuffer[_currentPosition++];
                }

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            } while (true);
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            int count = buffer.Length;
            int offset = 0;
            int bytesRead = 0;

            while (count > 0)
            {
                // check for end of stream.
                if (_currentBuffer.Array == null)
                {
                    return bytesRead;
                }

                int bytesLeft = GetBufferCount(_bufferIndex) - _currentPosition;

                // copy the bytes requested.
                if (bytesLeft > count)
                {
                    _currentBuffer.AsSpan(_currentPosition, count).CopyTo(buffer.Slice(offset));
                    bytesRead += count;
                    _currentPosition += count;
                    return bytesRead;
                }

                // copy the bytes available and move to next buffer.
                _currentBuffer.AsSpan(_currentPosition, bytesLeft).CopyTo(buffer.Slice(offset));
                bytesRead += bytesLeft;

                offset += bytesLeft;
                count -= bytesLeft;

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            }

            return bytesRead;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            while (count > 0)
            {
                // check for end of stream.
                if (_currentBuffer.Array == null)
                {
                    return bytesRead;
                }

                int bytesLeft = GetBufferCount(_bufferIndex) - _currentPosition;

                // copy the bytes requested.
                if (bytesLeft > count)
                {
                    Array.Copy(_currentBuffer.Array, _currentPosition + _currentBuffer.Offset, buffer, offset, count);
                    bytesRead += count;
                    _currentPosition += count;
                    return bytesRead;
                }

                // copy the bytes available and move to next buffer.
                Array.Copy(_currentBuffer.Array, _currentPosition + _currentBuffer.Offset, buffer, offset, bytesLeft);
                bytesRead += bytesLeft;

                offset += bytesLeft;
                count -= bytesLeft;

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            }

            return bytesRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        break;
                    }

                case SeekOrigin.Current:
                    {
                        offset += GetAbsolutePosition();
                        break;
                    }

                case SeekOrigin.End:
                    {
                        offset += GetAbsoluteLength();
                        break;
                    }
            }

            if (offset < 0)
            {
                throw new IOException("Cannot seek beyond the beginning of the stream.");
            }

            // special case
            if (offset == 0)
            {
                SetCurrentBuffer(0);
                return 0;
            }

            int position = (int)offset;

            if (position > GetAbsolutePosition())
            {
                CheckEndOfStream();
            }

            for (int ii = 0; ii < _buffers.Count; ii++)
            {
                int length = GetBufferCount(ii);

                if (offset <= length)
                {
                    SetCurrentBuffer(ii);
                    _currentPosition = (int)offset;
                    return position;
                }

                offset -= length;
            }

            throw new IOException("Cannot seek beyond the end of the stream.");
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            do
            {
                // allocate new buffer if necessary
                CheckEndOfStream();

                int bytesLeft = _currentBuffer.Count - _currentPosition;

                // copy the byte requested.
                if (bytesLeft >= 1)
                {
                    _currentBuffer[_currentPosition] = value;
                    UpdateCurrentPosition(1);

                    return;
                }

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            } while (true);
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int count = buffer.Length;
            int offset = 0;
            while (count > 0)
            {
                // check for end of stream.
                CheckEndOfStream();

                int bytesLeft = _currentBuffer.Count - _currentPosition;

                // copy the bytes requested.
                if (bytesLeft >= count)
                {
                    buffer.Slice(offset, count).CopyTo(_currentBuffer.AsSpan(_currentPosition));

                    UpdateCurrentPosition(count);

                    return;
                }

                // copy the bytes available and move to next buffer.
                buffer.Slice(offset, bytesLeft).CopyTo(_currentBuffer.AsSpan(_currentPosition));

                offset += bytesLeft;
                count -= bytesLeft;

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            }
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                // check for end of stream.
                CheckEndOfStream();

                int bytesLeft = _currentBuffer.Count - _currentPosition;

                // copy the bytes requested.
                if (bytesLeft >= count)
                {
                    Array.Copy(buffer, offset, _currentBuffer.Array!, _currentPosition + _currentBuffer.Offset, count);

                    UpdateCurrentPosition(count);

                    return;
                }

                // copy the bytes available and move to next buffer.
                Array.Copy(buffer, offset, _currentBuffer.Array!, _currentPosition + _currentBuffer.Offset, bytesLeft);

                offset += bytesLeft;
                count -= bytesLeft;

                // move to next buffer.
                SetCurrentBuffer(_bufferIndex + 1);
            }
        }

        /// <inheritdoc/>
        public override byte[] ToArray()
        {
            if (_buffers == null)
            {
                throw new ObjectDisposedException(nameof(ArrayPoolMemoryStream));
            }

            int absoluteLength = GetAbsoluteLength();
            if (absoluteLength == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] array = GC.AllocateUninitializedArray<byte>(absoluteLength);

            int offset = 0;
            foreach (var buffer in _buffers)
            {
                if (buffer.Array != null)
                {
                    int length = buffer.Count;
                    Array.Copy(buffer.Array, buffer.Offset, array, offset, length);
                    offset += length;
                }
            }

            return array;
        }

        /// <summary>
        /// Helper to benchmark the performance of the stream.
        /// </summary>
        internal void WriteMemoryStream(ReadOnlySpan<byte> buffer) => base.Write(buffer);

        /// <summary>
        /// Helper to benchmark the performance of the stream.
        /// </summary>
        internal int ReadMemoryStream(Span<byte> buffer) => base.Read(buffer);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_buffers != null)
                {
                    if (!_externalBuffersReadOnly)
                    {
                        foreach (var buffer in _buffers)
                        {
                            if (buffer.Array != null)
                            {
                                ArrayPool<byte>.Shared.Return(buffer.Array);
                            }
                        }
                    }

                    _buffers.Clear();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Update the current buffer count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCurrentPosition(int count)
        {
            _currentPosition += count;

            if (_bufferIndex == _buffers.Count - 1)
            {
                if (_endOfLastBuffer < _currentPosition)
                {
                    _endOfLastBuffer = _currentPosition;
                }
            }
        }

        /// <summary>
        /// Sets the current buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCurrentBuffer(int index)
        {
            if (index < 0 || index >= _buffers.Count)
            {
                _currentBuffer = default(ArraySegment<byte>);
                _currentPosition = 0;
                return;
            }

            _bufferIndex = index;
            _currentBuffer = _buffers[index];
            _currentPosition = 0;
        }

        /// <summary>
        /// Returns the total length in all buffers.
        /// </summary>
        private int GetAbsoluteLength()
        {
            int length = 0;

            for (int ii = 0; ii < _buffers.Count; ii++)
            {
                length += GetBufferCount(ii);
            }

            return length;
        }

        /// <summary>
        /// Returns the current position.
        /// </summary>
        private int GetAbsolutePosition()
        {
            // check if at end of stream.
            if (_currentBuffer.Array == null)
            {
                return GetAbsoluteLength();
            }

            // calculate position.
            int position = 0;

            for (int ii = 0; ii < _bufferIndex; ii++)
            {
                position += GetBufferCount(ii);
            }

            position += _currentPosition;

            return position;
        }

        /// <summary>
        /// Returns the number of bytes used in the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBufferCount(int index)
        {
            if (index == _buffers.Count - 1)
            {
                return _endOfLastBuffer;
            }

            return _buffers[index].Count;
        }

        /// <summary>
        /// Check if end of stream is reached and take new buffer if necessary.
        /// </summary>
        /// <exception cref="IOException">Throws if end of stream is reached.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckEndOfStream()
        {
            // check for end of stream.
            if (_currentBuffer.Array == null)
            {
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                _buffers.Add(new ArraySegment<byte>(newBuffer, _start, _count));
                _endOfLastBuffer = 0;

                SetCurrentBuffer(_buffers.Count - 1);
            }
        }

        /// <summary>
        /// Clears the buffers and resets the state variables.
        /// </summary>
        private void ClearBuffers()
        {
            _buffers.Clear();
            _bufferIndex = 0;
            _endOfLastBuffer = 0;
            SetCurrentBuffer(0);
        }
    }
}
