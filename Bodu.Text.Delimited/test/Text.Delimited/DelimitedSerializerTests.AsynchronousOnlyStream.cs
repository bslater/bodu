// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.AsynchronousOnlyStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A stream that supports only asynchronous reads and writes, as an ASP.NET Core request or response body does when
    /// synchronous I/O is disallowed, and that completes every asynchronous operation asynchronously.
    /// </summary>
    /// <remarks>
    /// <see cref="Read(byte[], int, int)" />, <see cref="Write(byte[], int, int)" /> and <see cref="Flush" /> throw
    /// <see cref="NotSupportedException" />, as do the span overloads, which the base class routes through them. Each
    /// asynchronous read or write first waits a millisecond without capturing the caller's
    /// <see cref="SynchronizationContext" />, so that it completes on a thread-pool thread rather than the caller's.
    /// </remarks>
    private sealed class AsynchronousOnlyStream
        : Stream
    {
        /// <summary>The content read from, or the bytes written to, the stream.</summary>
        private readonly MemoryStream _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousOnlyStream" /> class to write to.
        /// </summary>
        public AsynchronousOnlyStream()
        {
            _content = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousOnlyStream" /> class to read from.
        /// </summary>
        /// <param name="content">The content the stream returns.</param>
        public AsynchronousOnlyStream(byte[] content)
        {
            _content = new MemoryStream(content, writable: false);
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => _content.CanWrite;

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
        /// Returns the bytes written to the stream.
        /// </summary>
        /// <returns>A copy of the bytes written.</returns>
        public byte[] ToArray() =>
            _content.ToArray();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; synchronous I/O is disallowed.</exception>
        public override void Flush() =>
            throw new NotSupportedException("Synchronous operations are disallowed.");

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; synchronous I/O is disallowed.</exception>
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous operations are disallowed.");

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return _content.Read(buffer.Span);
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; synchronous I/O is disallowed.</exception>
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous operations are disallowed.");

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc />
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            _content.Write(buffer.Span);
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is not seekable.</exception>
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Always; the stream is not seekable.</exception>
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _content.Dispose();

            base.Dispose(disposing);
        }
    }
}
