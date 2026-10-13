// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSink.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Serialization;

/// <summary>
/// The destination of a line-format writer: a caller's <see cref="IBufferWriter{T}" />, written directly, or a
/// <see cref="Stream" />, written from an internal buffer on each flush, with the counts of the bytes committed and
/// pending.
/// </summary>
/// <remarks>
/// The DotEnv, INI and Delimited writers each hold one as a field and write through it; quoting and escaping stay with
/// each format, and the sink writes bytes and UTF-8 text as given. Writing to a buffer writer commits each byte as it
/// is written, so nothing is ever pending and a flush does nothing; writing to a stream stages the bytes until a flush,
/// which commits them.
/// </remarks>
internal struct Utf8OutputSink
{
    /// <summary>The destination buffer writer in direct mode, or the internal scratch buffer in stream mode.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The destination stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly Stream? _stream;

    /// <summary>The scratch buffer backing the stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly ArrayBufferWriter<byte>? _scratch;

    /// <summary>The number of bytes committed to the destination.</summary>
    private long _bytesCommitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8OutputSink" /> struct that writes directly to a buffer writer.
    /// </summary>
    /// <param name="output">
    /// The destination buffer writer, which the caller has checked is not <see langword="null" />.
    /// </param>
    internal Utf8OutputSink(IBufferWriter<byte> output)
    {
        _output = output;
        _stream = null;
        _scratch = null;
        _bytesCommitted = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8OutputSink" /> struct that writes to a stream on each flush.
    /// </summary>
    /// <param name="stream">
    /// The destination stream, which the caller has checked is not <see langword="null" />.
    /// </param>
    internal Utf8OutputSink(Stream stream)
    {
        _scratch = new ArrayBufferWriter<byte>();
        _output = _scratch;
        _stream = stream;
        _bytesCommitted = 0;
    }

    /// <summary>
    /// Gets the number of bytes committed to the destination.
    /// </summary>
    /// <value>
    /// Every byte written, in direct buffer-writer mode; the bytes flushed to the stream, in stream mode.
    /// </value>
    internal readonly long BytesCommitted => _bytesCommitted;

    /// <summary>
    /// Gets the number of bytes written but not yet flushed to the destination.
    /// </summary>
    /// <value>The pending byte count; always zero in direct buffer-writer mode.</value>
    internal readonly long BytesPending => _scratch?.WrittenCount ?? 0;

    /// <summary>
    /// Writes any pending bytes to the destination stream and commits them; does nothing in direct buffer-writer mode.
    /// </summary>
    internal void Flush()
    {
        if (_stream is null || _scratch is null)
            return;

        if (_scratch.WrittenCount > 0)
        {
            _stream.Write(_scratch.WrittenSpan);
            _bytesCommitted += _scratch.WrittenCount;
            _scratch.Clear();
        }
    }

    /// <summary>
    /// Writes the supplied bytes verbatim.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    internal void WriteRaw(scoped ReadOnlySpan<byte> bytes)
    {
        Span<byte> destination = _output.GetSpan(bytes.Length);
        bytes.CopyTo(destination);
        _output.Advance(bytes.Length);
        AccountDirect(bytes.Length);
    }

    /// <summary>
    /// Writes one byte.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    internal void WriteByte(byte value)
    {
        Span<byte> destination = _output.GetSpan(1);
        destination[0] = value;
        _output.Advance(1);
        AccountDirect(1);
    }

    /// <summary>
    /// Writes the supplied text as its UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text to write.</param>
    internal void WriteText(scoped ReadOnlySpan<char> text)
    {
        int byteCount = Encoding.UTF8.GetByteCount(text);
        Span<byte> destination = _output.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(text, destination);
        _output.Advance(written);
        AccountDirect(written);
    }

    /// <summary>
    /// Commits written bytes in direct buffer-writer mode, where there is no flush step; in stream mode a flush commits
    /// them instead.
    /// </summary>
    /// <param name="count">The number of bytes written.</param>
    private void AccountDirect(int count)
    {
        if (_stream is null)
            _bytesCommitted += count;
    }
}
