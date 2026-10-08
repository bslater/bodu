// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.FeedStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Threading.Channels;

namespace Bodu.Text.Delimited;

public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A read-only stream whose content the test supplies while it is being read, piece by piece, as a producer that
    /// keeps a connection open writes it, and which ends only when the test ends it.
    /// </summary>
    /// <remarks>
    /// An asynchronous read returns the rest of the current piece, or as much of it as the caller's buffer holds; it
    /// never joins two pieces. With no piece left, it waits until the test feeds another or ends the stream, and then
    /// returns zero. Synchronous reads are not supported, since the code under test reads asynchronously.
    /// </remarks>
    private sealed class FeedStream
        : Stream
    {
        /// <summary>The pieces fed but not yet started, in order.</summary>
        private readonly Channel<byte[]> _pieces = Channel.CreateUnbounded<byte[]>();

        /// <summary>The piece being read.</summary>
        private byte[] _current = [];

        /// <summary>The number of bytes of <see cref="_current" /> already read.</summary>
        private int _offset;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is not seekable.</exception>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is not seekable.</exception>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Makes a further piece of the content available to the reader.
        /// </summary>
        /// <param name="piece">The piece.</param>
        public void Feed(byte[] piece) => _pieces.Writer.TryWrite(piece);

        /// <summary>
        /// Ends the content, so that a read with no piece left returns zero.
        /// </summary>
        public void End() => _pieces.Writer.TryComplete();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is read asynchronously.</exception>
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (_offset == _current.Length)
            {
                if (!await _pieces.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    return 0;

                if (_pieces.Reader.TryRead(out byte[]? next))
                {
                    _current = next;
                    _offset = 0;
                }
            }

            int count = Math.Min(buffer.Length, _current.Length - _offset);
            _current.AsMemory(_offset, count).CopyTo(buffer);
            _offset += count;
            return count;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is not seekable.</exception>
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is read-only.</exception>
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is read-only.</exception>
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
