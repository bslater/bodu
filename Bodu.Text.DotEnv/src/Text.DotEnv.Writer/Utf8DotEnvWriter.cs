// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Provides a high-performance, forward-only writer that emits DotEnv text as UTF-8 bytes to an
/// <see cref="IBufferWriter{T}" /> or a <see cref="Stream" />. The writer is a <see langword="ref struct" />, so it
/// lives on the stack and cannot be boxed or captured.
/// </summary>
/// <remarks>
/// <para>
/// DotEnv is line-oriented, so the writer emits progressively: <see cref="WritePropertyName(string)" /> followed by
/// <see cref="WriteString(string)" /> produces one <c>KEY=VALUE</c> line, and <see cref="WriteComment(string)" />
/// produces one <c>#</c> comment line. <see cref="WriteStartObject" /> and <see cref="WriteEndObject" /> frame the
/// document to mirror the reader's token stream but emit no bytes of their own.
/// </para>
/// <para>
/// A value is emitted unquoted when it is safe to do so; a value that is empty is left after the <c>=</c>, and a value
/// containing whitespace at its edges, an inline-comment sequence, a quote, a backslash, or a line break is
/// double-quoted with the necessary escape sequences so a subsequent read round-trips it exactly.
/// </para>
/// </remarks>
public ref struct Utf8DotEnvWriter
{
    /// <summary>The destination buffer writer in direct mode, or the internal scratch buffer in stream mode.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The destination stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly Stream? _stream;

    /// <summary>The scratch buffer backing the stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly ArrayBufferWriter<byte>? _scratch;

    /// <summary>The writer options.</summary>
    private readonly DotEnvWriterOptions _options;

    /// <summary>The number of bytes flushed to the destination.</summary>
    private long _bytesCommitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvWriter" /> struct that writes to the supplied buffer.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8DotEnvWriter(IBufferWriter<byte> output)
        : this(output, DotEnvWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvWriter" /> struct that writes to the supplied buffer
    /// using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8DotEnvWriter(IBufferWriter<byte> output, DotEnvWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);

        _output = output;
        _stream = null;
        _scratch = null;
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvWriter" /> struct that writes to the supplied stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public Utf8DotEnvWriter(Stream stream)
        : this(stream, DotEnvWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvWriter" /> struct that writes to the supplied stream
    /// using the supplied options.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public Utf8DotEnvWriter(Stream stream, DotEnvWriterOptions options)
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
    /// Writes the document object start. DotEnv has no object delimiter, so this method emits no bytes; it exists to
    /// mirror the reader's token stream.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method by design: it is part of the writer's token-stream surface and mirrors the reader.")]
    public readonly void WriteStartObject()
    {
    }

    /// <summary>
    /// Writes the document object end. DotEnv has no object delimiter, so this method emits no bytes; it exists to
    /// mirror the reader's token stream.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method by design: it is part of the writer's token-stream surface and mirrors the reader.")]
    public readonly void WriteEndObject()
    {
    }

    /// <summary>
    /// Writes a key name, followed by the <c>=</c> assignment. Call <see cref="WriteString(string)" /> next to write
    /// the value.
    /// </summary>
    /// <param name="name">The key name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The <c>export</c> prefix is emitted when <see cref="DotEnvWriterOptions.WriteExportPrefix" /> is set.
    /// </remarks>
    public void WritePropertyName(string name) =>
        WritePropertyName(name, _options.WriteExportPrefix);

    /// <summary>
    /// Writes a key name with an explicit <c>export</c> decision, followed by the <c>=</c> assignment. Call
    /// <see cref="WriteString(string)" /> next to write the value.
    /// </summary>
    /// <param name="name">The key name.</param>
    /// <param name="export"><see langword="true" /> to prefix the key with the <c>export</c> keyword.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public void WritePropertyName(string name, bool export)
    {
        ThrowHelper.ThrowIfNull(name);

        if (export)
            WriteRaw("export "u8);

        WriteText(name);
        WriteRaw("="u8);
    }

    /// <summary>
    /// Writes a string value, quoting and escaping it when required, followed by a line feed.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (NeedsQuoting(value))
            WriteQuoted(value);
        else
            WriteText(value);

        WriteRaw("\n"u8);
    }

    /// <summary>
    /// Writes a comment line prefixed with <c>#</c>, followed by a line feed.
    /// </summary>
    /// <param name="text">The comment text, without the leading prefix.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public void WriteComment(string text)
    {
        ThrowHelper.ThrowIfNull(text);

        WriteRaw("#"u8);
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
    /// Writes a double-quoted, escaped rendering of the supplied value.
    /// </summary>
    /// <param name="value">The value to quote.</param>
    private void WriteQuoted(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append('\\').Append('"'); break;
                case '\\': sb.Append('\\').Append('\\'); break;
                case '\n': sb.Append('\\').Append('n'); break;
                case '\r': sb.Append('\\').Append('r'); break;
                case '\t': sb.Append('\\').Append('t'); break;
                default: sb.Append(c); break;
            }
        }

        sb.Append('"');
        WriteText(sb.ToString());
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
    /// Attributes written bytes to the committed count in direct buffer-writer mode, where there is no flush step. In
    /// stream mode the committed count is tracked at flush time instead.
    /// </summary>
    /// <param name="count">The number of bytes written.</param>
    private void AccountDirect(int count)
    {
        if (_stream is null)
            _bytesCommitted += count;
    }

    /// <summary>
    /// Determines whether a value must be double-quoted to round-trip safely.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when the value requires quoting.</returns>
    private static bool NeedsQuoting(string value)
    {
        if (value.Length == 0)
            return false;

        if (value[0] is ' ' or '\t' || value[^1] is ' ' or '\t')
            return true;

        if (value[0] is '"' or '\'')
            return true;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is '\n' or '\r' or '"' or '\\')
                return true;

            if (c == '#' && i > 0 && value[i - 1] is ' ' or '\t')
                return true;
        }

        return false;
    }
}
