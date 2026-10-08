// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Provides a forward-only writer that emits INI text as UTF-8 bytes to an <see cref="IBufferWriter{T}" /> or a
/// <see cref="Stream" />. The writer is a <see langword="ref struct" />.
/// </summary>
/// <remarks>
/// <para>
/// INI is line-oriented, so the writer emits progressively: <see cref="WriteSectionHeader(string)" /> produces a
/// <c>[name]</c> line, <see cref="WritePropertyName(string)" /> followed by <see cref="WriteString(string)" /> produces
/// one <c>key=value</c> line, and <see cref="WriteComment(string)" /> produces one comment line per line of its text.
/// </para>
/// <para>
/// The writer writes only text that <see cref="Bodu.Text.Ini.Reader.Utf8IniReader" /> reads back unchanged, and throws
/// <see cref="ArgumentException" /> for anything else: a value containing a line break, which a <c>key=value</c> line
/// cannot hold, or beginning or ending with a space or a tab, which the reader trims.
/// </para>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> contains a carriage return or a line feed, which a <c>key=value</c> line
    /// cannot hold, or begins or ends with a space or a tab, which the reader trims.
    /// </exception>
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowIfContainsLineBreak(value);
        ThrowIfSurroundedByWhitespace(value);

        WriteText(value);
        WriteRaw("\n"u8);
    }

    /// <summary>
    /// Writes a comment as one comment line per line of its text, each prefixed with the configured comment character
    /// and followed by a line feed.
    /// </summary>
    /// <param name="text">The comment text, without the leading prefix.</param>
    /// <remarks>
    /// A carriage return, a line feed, or a carriage return followed by a line feed ends a line of the text, as it ends
    /// a line for <see cref="Bodu.Text.Ini.Reader.Utf8IniReader" />, so each line reads back as a comment of its own;
    /// an empty line becomes an empty comment line.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public void WriteComment(string text)
    {
        ThrowHelper.ThrowIfNull(text);

        ReadOnlySpan<char> remaining = text;
        int lineBreak;
        while ((lineBreak = remaining.IndexOfAny('\r', '\n')) >= 0)
        {
            WriteCommentLine(remaining[..lineBreak]);

            int next = lineBreak + 1;
            if (remaining[lineBreak] == '\r' && next < remaining.Length && remaining[next] == '\n')
                next++;

            remaining = remaining[next..];
        }

        WriteCommentLine(remaining);
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
    /// Throws when text contains a line break, which a single INI line cannot hold.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text" /> contains a carriage return or a line feed.
    /// </exception>
    private static void ThrowIfContainsLineBreak(string text, [CallerArgumentExpression(nameof(text))] string? paramName = null)
    {
        if (text.AsSpan().ContainsAny('\r', '\n')) throw new ArgumentException(IniResourceStrings.Arg_Invalid_IniLineBreak, paramName);
    }

    /// <summary>
    /// Throws when text begins or ends with a space or a tab, the whitespace
    /// <see cref="Bodu.Text.Ini.Reader.Utf8IniReader" /> trims from keys, values and section names.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text" /> begins or ends with a space or a tab.
    /// </exception>
    private static void ThrowIfSurroundedByWhitespace(string text, [CallerArgumentExpression(nameof(text))] string? paramName = null)
    {
        if (text.Length > 0 && (text[0] is ' ' or '\t' || text[^1] is ' ' or '\t')) throw new ArgumentException(IniResourceStrings.Arg_Invalid_IniSurroundingWhitespace, paramName);
    }

    /// <summary>
    /// Writes one comment line: the configured comment prefix, the text and a line feed.
    /// </summary>
    /// <param name="line">The text of the line, which holds no line break.</param>
    private void WriteCommentLine(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> prefix = [_options.EffectiveCommentPrefix];
        WriteText(prefix);
        WriteText(line);
        WriteRaw("\n"u8);
    }

    /// <summary>
    /// Writes the supplied text as UTF-8 bytes verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    private void WriteText(scoped ReadOnlySpan<char> text)
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
