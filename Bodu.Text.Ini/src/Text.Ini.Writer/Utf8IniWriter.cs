// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Provides a forward-only writer that emits INI text as UTF-8 bytes to an <see cref="IBufferWriter{T}" /> or a
/// <see cref="Stream" />. The writer is a <see langword="ref struct" />.
/// </summary>
/// <remarks>
/// INI is line-oriented, so the writer emits progressively: <see cref="WriteSectionHeader(string)" /> produces a
/// <c>[name]</c> line, <see cref="WritePropertyName(string)" /> followed by <see cref="WriteString(string)" /> produces
/// one <c>key=value</c> line, and <see cref="WriteComment(string)" /> produces one comment line.
/// </remarks>
public ref struct Utf8IniWriter
{
    /// <summary>The destination buffer writer, or the scratch buffer in stream mode.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The destination stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly Stream? _stream;

    /// <summary>The scratch buffer backing the stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly ArrayBufferWriter<byte>? _scratch;

    /// <summary>The writer options.</summary>
    private readonly IniWriterOptions _options;

    /// <summary>The number of bytes flushed to the destination.</summary>
    private long _bytesCommitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniWriter" /> struct that writes to the supplied buffer.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8IniWriter(IBufferWriter<byte> output)
        : this(output, IniWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniWriter" /> struct that writes to the supplied buffer using
    /// the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8IniWriter(IBufferWriter<byte> output, IniWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _stream = null;
        _scratch = null;
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniWriter" /> struct that writes to the supplied stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public Utf8IniWriter(Stream stream)
        : this(stream, IniWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniWriter" /> struct that writes to the supplied stream using
    /// the supplied options.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public Utf8IniWriter(Stream stream, IniWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);

        _scratch = new ArrayBufferWriter<byte>();
        _output = _scratch;
        _stream = stream;
        _options = options;
    }

    /// <summary>
    /// Gets the number of bytes flushed to the destination.
    /// </summary>
    /// <value>The committed byte count.</value>
    public readonly long BytesCommitted => _bytesCommitted;

    /// <summary>
    /// Gets the number of bytes written but not yet flushed to the destination.
    /// </summary>
    /// <value>The pending byte count; always zero in direct buffer-writer mode.</value>
    public readonly long BytesPending => _scratch?.WrittenCount ?? 0;

    /// <summary>
    /// Writes a section header line (<c>[name]</c>).
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public void WriteSectionHeader(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        WriteRaw("["u8);
        WriteText(name);
        WriteRaw("]\n"u8);
    }

    /// <summary>
    /// Writes a key name, followed by the <c>=</c> assignment. Call <see cref="WriteString(string)" /> next to write
    /// the value.
    /// </summary>
    /// <param name="name">The key name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        WriteText(name);
        WriteRaw("="u8);
    }

    /// <summary>
    /// Writes a string value, followed by a line feed.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        WriteText(value);
        WriteRaw("\n"u8);
    }

    /// <summary>
    /// Writes a comment line prefixed with the configured comment character, followed by a line feed.
    /// </summary>
    /// <param name="text">The comment text, without the leading prefix.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public void WriteComment(string text)
    {
        ThrowHelper.ThrowIfNull(text);

        Span<char> prefix = [_options.EffectiveCommentPrefix];
        WriteText(new string(prefix));
        WriteText(text);
        WriteRaw("\n"u8);
    }

    /// <summary>
    /// Flushes any buffered bytes to the destination stream. This method is a no-op in direct buffer-writer mode.
    /// </summary>
    public void Flush()
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
    /// Flushes any buffered bytes and releases the writer.
    /// </summary>
    public void Dispose() => Flush();

    /// <summary>
    /// Writes the supplied text as UTF-8 bytes verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    private void WriteText(string text)
    {
        int byteCount = Encoding.UTF8.GetByteCount(text);
        Span<byte> destination = _output.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(text, destination);
        _output.Advance(written);
        AccountDirect(written);
    }

    /// <summary>
    /// Writes the supplied UTF-8 bytes verbatim.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    private void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        Span<byte> destination = _output.GetSpan(bytes.Length);
        bytes.CopyTo(destination);
        _output.Advance(bytes.Length);
        AccountDirect(bytes.Length);
    }

    /// <summary>
    /// Attributes written bytes to the committed count in direct buffer-writer mode.
    /// </summary>
    /// <param name="count">The number of bytes written.</param>
    private void AccountDirect(int count)
    {
        if (_stream is null)
            _bytesCommitted += count;
    }
}
