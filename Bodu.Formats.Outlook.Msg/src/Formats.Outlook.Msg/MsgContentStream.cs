// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgContentStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Wraps a container stream served to a caller so that a container fault raised during a read surfaces as
/// <see cref="OutlookMsgFormatException" /> rather than <see cref="CompoundFileException" />.
/// </summary>
/// <remarks>
/// Under the streaming read strategy a <see cref="CompoundStream" /> walks its sector chain on demand, so a corrupt
/// chain can fail part-way through a read long after the attachment was opened. This wrapper keeps the reader's
/// documented exception contract for that path; it is read-only and seekable exactly as the wrapped stream is.
/// </remarks>
internal sealed class MsgContentStream : Stream
{
    /// <summary>The container stream.</summary>
    private readonly Stream _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsgContentStream" /> class.
    /// </summary>
    /// <param name="inner">The container stream to wrap; disposed with this instance.</param>
    internal MsgContentStream(Stream inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public override bool CanRead =>
        _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek =>
        _inner.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite =>
        false;

    /// <inheritdoc />
    public override long Length =>
        Guard(static s => s.Length);

    /// <inheritdoc />
    public override long Position
    {
        get => _inner.Position;
        set => Guard(s => s.Position = value);
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        Guard(s => s.Read(buffer, offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        try
        {
            return _inner.Read(buffer);
        }
        catch (CompoundFileException ex)
        {
            throw MsgContainer.Wrap(ex);
        }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        Guard(s => s.Seek(offset, origin));

    /// <inheritdoc />
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Runs an operation against the wrapped stream, translating a container fault.
    /// </summary>
    /// <typeparam name="T">The operation's result type.</typeparam>
    /// <param name="operation">The operation.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="OutlookMsgFormatException">The container is malformed.</exception>
    private T Guard<T>(Func<Stream, T> operation)
    {
        try
        {
            return operation(_inner);
        }
        catch (CompoundFileException ex)
        {
            throw MsgContainer.Wrap(ex);
        }
    }
}
