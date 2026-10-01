// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.DetachableWriteStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class ICryptoTransformExtensions
{
    /// <summary>
    /// A write-only pass-through over a caller's stream that can be detached, after which it discards every write and
    /// flush instead of forwarding it.
    /// </summary>
    /// <remarks>
    /// The stream overloads of <see cref="Transform(ICryptoTransform, Stream, Stream, int)" /> and
    /// <see cref="TransformAsync(ICryptoTransform, Stream, Stream, int, CancellationToken)" /> write their
    /// <see cref="CryptoStream" /> through this wrapper. When the operation fails or is cancelled they detach it before
    /// disposing the <see cref="CryptoStream" />: disposal still clears the <see cref="CryptoStream" />'s internal
    /// buffers, but the final block it flushes is discarded, so a partial output never ends in a padded block that
    /// could pass for a complete ciphertext.
    /// </remarks>
    private sealed class DetachableWriteStream
        : Stream
    {
        /// <summary>The caller's stream that writes are forwarded to until the wrapper is detached.</summary>
        private readonly Stream _target;

        /// <summary>A value indicating whether the wrapper has been detached and now discards writes.</summary>
        private bool _detached;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetachableWriteStream" /> class over the specified stream.
        /// </summary>
        /// <param name="target">The caller's stream that receives forwarded writes.</param>
        public DetachableWriteStream(Stream target) =>
            _target = target;

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => _target.CanWrite;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Stops forwarding to the caller's stream; every later write or flush is discarded.
        /// </summary>
        public void Detach() =>
            _detached = true;

        /// <inheritdoc />
        public override void Flush()
        {
            if (!_detached)
                _target.Flush();
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _detached ? Task.CompletedTask : _target.FlushAsync(cancellationToken);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_detached)
                _target.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!_detached)
                _target.Write(buffer);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _detached ? Task.CompletedTask : _target.WriteAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            _detached ? ValueTask.CompletedTask : _target.WriteAsync(buffer, cancellationToken);
    }
}
