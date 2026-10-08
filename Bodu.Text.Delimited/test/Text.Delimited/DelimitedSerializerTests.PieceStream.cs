// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.PieceStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A read-only stream that returns its content in the pieces it was given, one piece per read, as a network source
    /// delivers its packets, and that can hold an asynchronous read at the end of its content until it is told the
    /// source has ended.
    /// </summary>
    /// <remarks>
    /// A read returns the rest of the current piece, or as much of it as the caller's buffer holds; it never joins two
    /// pieces, and an empty piece is skipped. Once every piece is read, an asynchronous read waits for the end-of-stream
    /// signal before it returns zero; a synchronous read returns zero at once.
    /// </remarks>
    private sealed class PieceStream
        : Stream
    {
        /// <summary>The pieces not yet started, in order.</summary>
        private readonly Queue<byte[]> _pieces;

        /// <summary>The task whose completion lets an asynchronous read report the end of the stream.</summary>
        private readonly Task _endOfStream;

        /// <summary>The piece being read.</summary>
        private byte[] _current = [];

        /// <summary>The number of bytes of <see cref="_current" /> already read.</summary>
        private int _offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="PieceStream" /> class.
        /// </summary>
        /// <param name="pieces">The pieces of the content, in order.</param>
        /// <param name="endOfStream">
        /// A task that completes when the source ends, or <see langword="null" /> for a source that ends with its last
        /// piece.
        /// </param>
        public PieceStream(IEnumerable<byte[]> pieces, Task? endOfStream = null)
        {
            _pieces = new Queue<byte[]>(pieces);
            _endOfStream = endOfStream ?? Task.CompletedTask;
        }

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

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            while (_offset == _current.Length)
            {
                if (!_pieces.TryDequeue(out byte[]? next))
                    return 0;

                _current = next;
                _offset = 0;
            }

            int count = Math.Min(buffer.Length, _current.Length - _offset);
            _current.AsSpan(_offset, count).CopyTo(buffer);
            _offset += count;
            return count;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = Read(buffer.Span);
            if (read == 0 && buffer.Length > 0)
                await _endOfStream.WaitAsync(cancellationToken).ConfigureAwait(false);

            return read;
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
