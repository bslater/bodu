// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides a forward-only, streaming reader for DotEnv-format text, surfacing one key/value entry at a time from a
/// <see cref="TextReader" /> without loading the entire source into memory.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DotEnvReader" /> is the streaming counterpart to <see cref="DotEnv.Parse(ReadOnlySpan{char})" />. Because
/// a double-quoted value may contain embedded newlines and <c>\</c> line-continuations, the reader buffers input
/// incrementally rather than line by line, parsing exactly one entry per advance using the same grammar as
/// <see cref="DotEnv.Parse(ReadOnlySpan{char})" />.
/// </para>
/// <para>
/// Call <see cref="Read" /> (or <see cref="ReadAsync" />) repeatedly to advance through entries. When it returns
/// <see langword="true" />, <see cref="Key" /> and <see cref="Value" /> reflect the current entry; when it returns
/// <see langword="false" />, the input is exhausted. Comments and blank lines are skipped. The reader surfaces entries
/// in source order and does <b>not</b> apply the document-level <see cref="DotEnvParseOptions.DuplicateKeyBehavior" />
/// policy, which requires the whole document and remains <see cref="DotEnv.Parse(ReadOnlySpan{char})" />'s
/// responsibility.
/// </para>
/// <para>
/// The reader takes ownership of the supplied <see cref="TextReader" /> and disposes it when <see cref="Dispose" /> is
/// called.
/// </para>
/// </remarks>
public sealed class DotEnvReader
    : IDisposable
{
    /// <summary>The default size of the internal character buffer in characters used when no buffer size is supplied.</summary>
    public const int DefaultBufferSize = 4096;

    /// <summary>The maximum number of characters a single entry may occupy in the pending buffer before it resolves. Internal so tests can validate the bound.</summary>
    /// <remarks>
    /// The reader re-parses the pending buffer from its start on every refill, so an oversized or unterminated
    /// construct (for example a double-quoted value with no closing quote) drives quadratic work as it grows without
    /// bound. Capping the unresolved span bounds that work against untrusted input while admitting any realistic entry
    /// (this is far larger than any legitimate DotEnv value).
    /// </remarks>
    internal const int MaxPendingLength = 1 << 20;

    /// <summary>The underlying text reader supplying the DotEnv input. Owned by this instance.</summary>
    private readonly TextReader _reader;

    /// <summary>The parse options that govern quoting, escaping, and interpolation behavior.</summary>
    private readonly DotEnvParseOptions _options;

    /// <summary>The internal character buffer that holds characters read from the underlying reader.</summary>
    private readonly char[] _buffer;

    /// <summary>The buffered text carried over from a previous read that has not yet formed a complete line.</summary>
    private string _pending = string.Empty;

    /// <summary>Indicates whether the end of the underlying reader has been reached.</summary>
    private bool _eof;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>The number of source lines consumed so far.</summary>
    private int _consumedLines;

    /// <summary>The key of the entry produced by the most recent successful read.</summary>
    private string _key = string.Empty;

    /// <summary>The value of the entry produced by the most recent successful read.</summary>
    private string _value = string.Empty;

    /// <summary>The one-based line number of the entry produced by the most recent successful read.</summary>
    private int _lineNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvReader" /> class with default options and buffer size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read DotEnv text from. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public DotEnvReader(TextReader reader)
        : this(reader, DotEnvParseOptions.Default, DefaultBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvReader" /> class with the specified options and the default
    /// buffer size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read DotEnv text from. Owned by this instance.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public DotEnvReader(TextReader reader, DotEnvParseOptions options)
        : this(reader, options, DefaultBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvReader" /> class with the specified options and buffer size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read DotEnv text from. Owned by this instance.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="bufferSize">
    /// The number of characters read from the underlying reader per fill. Must be at least 1.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize" /> is less than 1.
    /// </exception>
    public DotEnvReader(TextReader reader, DotEnvParseOptions options, int bufferSize)
    {
        ThrowHelper.ThrowIfNull(reader);
        Bodu.ThrowHelper.ThrowIfLessThan(bufferSize, 1);

        _reader = reader;
        _options = options;
        _buffer = new char[bufferSize];
    }

    /// <summary>
    /// Gets the key of the current entry. Valid only when the most recent call to <see cref="Read" /> or
    /// <see cref="ReadAsync" /> returned <see langword="true" />.
    /// </summary>
    /// <value>The current entry key.</value>
    public string Key => _key;

    /// <summary>
    /// Gets the value of the current entry. Valid only when the most recent call to <see cref="Read" /> or
    /// <see cref="ReadAsync" /> returned <see langword="true" />.
    /// </summary>
    /// <value>The current entry value, with quotes stripped and escape sequences resolved.</value>
    public string Value => _value;

    /// <summary>
    /// Gets the 1-based line number on which the current entry's key appears.
    /// </summary>
    /// <value>The current entry's line number, starting at 1.</value>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Advances the reader to the next key/value entry, skipping comments and blank lines.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when an entry was read and <see cref="Key" />/<see cref="Value" /> reflect it;
    /// <see langword="false" /> when the input is exhausted.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="DotEnvFormatException">Thrown when the source is malformed.</exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            ReadResult result = TryReadOneEntry(
                _pending, _eof, _options, 1 + _consumedLines,
                out string? key, out string? value, out int consumed, out int lineDelta, out int keyLineDelta);

            if (result == ReadResult.Entry)
            {
                Commit(key, value, consumed, lineDelta, keyLineDelta);
                return true;
            }

            if (result == ReadResult.End)
                return false;

            // NeedMore: reject an entry that has outgrown the cap before pulling more input, so an oversized or
            // unterminated construct cannot drive unbounded quadratic re-parsing.
            if (_pending.Length > MaxPendingLength)
                DotEnv.ThrowEntryTooLong(MaxPendingLength, 1 + _consumedLines);

            // Pull another chunk; mark EOF when the reader is drained.
            int n = _reader.Read(_buffer, 0, _buffer.Length);
            if (n == 0)
                _eof = true;
            else
                _pending += new string(_buffer, 0, n);
        }
    }

    /// <summary>
    /// Asynchronously advances the reader to the next key/value entry, skipping comments and blank lines.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
    /// <returns>
    /// A task that completes with <see langword="true" /> when an entry was read and <see cref="Key" />/
    /// <see cref="Value" /> reflect it; <see langword="false" /> when the input is exhausted.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="DotEnvFormatException">Thrown when the source is malformed.</exception>
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            ReadResult result = TryReadOneEntry(
                _pending, _eof, _options, 1 + _consumedLines,
                out string? key, out string? value, out int consumed, out int lineDelta, out int keyLineDelta);

            if (result == ReadResult.Entry)
            {
                Commit(key, value, consumed, lineDelta, keyLineDelta);
                return true;
            }

            if (result == ReadResult.End)
                return false;

            // NeedMore: reject an entry that has outgrown the cap before pulling more input, so an oversized or
            // unterminated construct cannot drive unbounded quadratic re-parsing.
            if (_pending.Length > MaxPendingLength)
                DotEnv.ThrowEntryTooLong(MaxPendingLength, 1 + _consumedLines);

            int n = await _reader.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (n == 0)
                _eof = true;
            else
                _pending += new string(_buffer, 0, n);
        }
    }

    /// <summary>
    /// Releases the underlying <see cref="TextReader" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _reader.Dispose();
    }

    /// <summary>
    /// Commits a successfully parsed entry: publishes its key/value/line, advances the consumed-line counter, and drops
    /// the consumed prefix from the pending buffer.
    /// </summary>
    /// <param name="key">The parsed entry key.</param>
    /// <param name="value">The parsed entry value.</param>
    /// <param name="consumed">The number of characters consumed from the pending buffer.</param>
    /// <param name="lineDelta">The number of newlines consumed by the entry.</param>
    /// <param name="keyLineDelta">The number of newlines preceding the key within the consumed span.</param>
    private void Commit(string key, string value, int consumed, int lineDelta, int keyLineDelta)
    {
        _key = key;
        _value = value;
        _lineNumber = 1 + _consumedLines + keyLineDelta;
        _consumedLines += lineDelta;
        _pending = _pending[consumed..];
    }

    /// <summary>
    /// The outcome of a single-entry parse attempt over the pending buffer.
    /// </summary>
    private enum ReadResult
    {
        /// <summary>
        /// A complete entry was parsed.
        /// </summary>
        Entry,

        /// <summary>
        /// More input is required before an entry can be resolved.
        /// </summary>
        NeedMore,

        /// <summary>
        /// The input is exhausted with no further entries.
        /// </summary>
        End,
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="c" /> may start a key.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="c" /> is an ASCII letter or underscore; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsKeyStart(char c) =>
        c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="c" /> may continue a key.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="c" /> is a key start character or an ASCII digit; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsKeyContinue(char c) =>
        IsKeyStart(c) || (c >= '0' && c <= '9');

    /// <summary>
    /// Attempts to parse exactly one entry from the head of <paramref name="s" />, mirroring the grammar of
    /// <see cref="DotEnv.Parse(ReadOnlySpan{char})" />. When the buffer ends inside an incomplete construct and
    /// <paramref name="isFinal" /> is <see langword="false" />, returns <see cref="ReadResult.NeedMore" /> without
    /// consuming input.
    /// </summary>
    /// <param name="s">The pending input.</param>
    /// <param name="isFinal">
    /// Whether <paramref name="s" /> is the final block (the reader has reached end of stream).
    /// </param>
    /// <param name="options">The parse options.</param>
    /// <param name="baseLine">The 1-based line number of the first character in <paramref name="s" />.</param>
    /// <param name="key">When an entry is parsed, receives its key.</param>
    /// <param name="value">When an entry is parsed, receives its value.</param>
    /// <param name="consumed">When an entry is parsed, receives the number of characters consumed.</param>
    /// <param name="lineDelta">When an entry is parsed, receives the number of newlines consumed.</param>
    /// <param name="keyLineDelta">
    /// When an entry is parsed, receives the number of newlines skipped before the key.
    /// </param>
    /// <returns>The parse outcome.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the source is malformed.</exception>
    private static ReadResult TryReadOneEntry(
        ReadOnlySpan<char> s, bool isFinal, DotEnvParseOptions options, int baseLine,
        out string key, out string value, out int consumed, out int lineDelta, out int keyLineDelta)
    {
        key = string.Empty;
        value = string.Empty;
        consumed = 0;
        lineDelta = 0;
        keyLineDelta = 0;

        int i = 0;
        int line = 0;

        // Skip leading whitespace, blank lines, and comment lines until a key candidate is found.
        while (true)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t'))
                i++;

            if (i >= s.Length)
                return isFinal ? ReadResult.End : ReadResult.NeedMore;

            char c = s[i];

            if (c == '\n')
            {
                i++;
                line++;
                continue;
            }

            if (c == '\r')
            {
                if (i + 1 >= s.Length && !isFinal)
                    return ReadResult.NeedMore;

                i++;
                if (i < s.Length && s[i] == '\n')
                    i++;
                line++;
                continue;
            }

            if (c == '#')
            {
                int j = i;
                while (j < s.Length && s[j] != '\n' && s[j] != '\r')
                    j++;

                if (j >= s.Length && !isFinal)
                    return ReadResult.NeedMore;

                i = j;
                if (i < s.Length)
                {
                    if (s[i] == '\r')
                    {
                        if (i + 1 >= s.Length && !isFinal)
                            return ReadResult.NeedMore;
                        i++;
                        if (i < s.Length && s[i] == '\n')
                            i++;
                    }
                    else
                    {
                        i++;
                    }

                    line++;
                }

                continue;
            }

            break;
        }

        keyLineDelta = line;
        int entryLine = baseLine + line;

        // Optional "export " prefix.
        if (options.AllowExportPrefix)
        {
            if (s.Length - i < 7)
            {
                if (!isFinal)
                    return ReadResult.NeedMore;
            }
            else if (s.Slice(i, 6).SequenceEqual("export".AsSpan()) && (s[i + 6] == ' ' || s[i + 6] == '\t'))
            {
                i += 6;
                while (i < s.Length && (s[i] == ' ' || s[i] == '\t'))
                    i++;

                if (i >= s.Length && !isFinal)
                    return ReadResult.NeedMore;
            }
        }

        // Key.
        if (i >= s.Length || !IsKeyStart(s[i]))
        {
            if (i >= s.Length && !isFinal)
                return ReadResult.NeedMore;

            string badToken = i >= s.Length ? string.Empty : s[i].ToString();
            DotEnv.ThrowInvalidKey(badToken, entryLine);
        }

        int keyStart = i;
        i++;
        while (i < s.Length && IsKeyContinue(s[i]))
            i++;

        if (i >= s.Length && !isFinal)
            return ReadResult.NeedMore;

        key = s[keyStart..i].ToString();

        // Expect '='.
        if (i >= s.Length)
        {
            if (!isFinal)
                return ReadResult.NeedMore;
            DotEnv.ThrowMalformedEntry(entryLine);
        }

        if (s[i] != '=')
            DotEnv.ThrowMalformedEntry(entryLine);

        i++;

        // Value dispatch by leading delimiter.
        if (i >= s.Length && !isFinal)
            return ReadResult.NeedMore;

        char lead = i < s.Length ? s[i] : '\0';

        ReadResult valueResult = lead switch
        {
            '"' => ParseDoubleQuoted(s, ref i, ref line, isFinal, entryLine, out value),
            '\'' => ParseSingleQuoted(s, ref i, isFinal, entryLine, out value),
            _ => ParseUnquoted(s, ref i, isFinal, options, out value),
        };

        if (valueResult == ReadResult.NeedMore)
            return ReadResult.NeedMore;

        // Consume any trailing characters and the line terminator.
        while (i < s.Length && s[i] != '\n' && s[i] != '\r')
            i++;

        if (i >= s.Length && !isFinal)
            return ReadResult.NeedMore;

        if (i < s.Length)
        {
            if (s[i] == '\r')
            {
                if (i + 1 >= s.Length && !isFinal)
                    return ReadResult.NeedMore;
                i++;
                if (i < s.Length && s[i] == '\n')
                    i++;
            }
            else
            {
                i++;
            }

            line++;
        }

        consumed = i;
        lineDelta = line;
        return ReadResult.Entry;
    }

    /// <summary>
    /// Parses a double-quoted value (with escape sequences and literal/continuation newlines) starting at the opening
    /// quote. Returns <see cref="ReadResult.NeedMore" /> when the closing quote has not yet been buffered.
    /// </summary>
    /// <param name="s">The pending input.</param>
    /// <param name="i">
    /// The current read offset at the opening quote; advanced past the closing quote on success.
    /// </param>
    /// <param name="line">The current line counter; advanced for each newline consumed within the value.</param>
    /// <param name="isFinal">Whether <paramref name="s" /> is the final block of input.</param>
    /// <param name="entryLine">The 1-based line number on which the entry begins.</param>
    /// <param name="value">When an entry is parsed, receives the processed string value.</param>
    /// <returns>
    /// <see cref="ReadResult.Entry" /> when the value was parsed; otherwise, <see cref="ReadResult.NeedMore" /> when
    /// more input is required.
    /// </returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated and <paramref name="isFinal" /> is <see langword="true" />.
    /// </exception>
    private static ReadResult ParseDoubleQuoted(ReadOnlySpan<char> s, ref int i, ref int line, bool isFinal, int entryLine, out string value)
    {
        value = string.Empty;
        int start = i;
        int startLine = line;
        i++; // opening quote

        StringBuilder sb = new();

        while (true)
        {
            if (i >= s.Length)
            {
                if (isFinal)
                    DotEnv.ThrowUnterminatedDoubleQuote(entryLine);

                i = start;
                line = startLine;
                return ReadResult.NeedMore;
            }

            char c = s[i];

            if (c == '"')
            {
                i++;
                value = sb.ToString();
                return ReadResult.Entry;
            }

            if (c == '\\')
            {
                if (i + 1 >= s.Length)
                {
                    if (isFinal)
                        DotEnv.ThrowUnterminatedDoubleQuote(entryLine);

                    i = start;
                    line = startLine;
                    return ReadResult.NeedMore;
                }

                i++; // consume backslash
                char esc = s[i];
                i++;
                if (esc == '\n')
                    line++;

                switch (esc)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '$': sb.Append('$'); break;
                    case '\n': break; // line continuation
                    case '\r':
                        if (i >= s.Length && !isFinal)
                        {
                            i = start;
                            line = startLine;
                            return ReadResult.NeedMore;
                        }

                        if (i < s.Length && s[i] == '\n')
                        {
                            i++;
                            line++;
                        }

                        break;
                    default:
                        sb.Append('\\');
                        sb.Append(esc);
                        break;
                }

                continue;
            }

            if (c == '\n')
                line++;

            sb.Append(c);
            i++;
        }
    }

    /// <summary>
    /// Parses a single-quoted (literal) value starting at the opening quote. The value must close on the same line.
    /// Returns <see cref="ReadResult.NeedMore" /> when the closing quote has not yet been buffered.
    /// </summary>
    /// <param name="s">The pending input.</param>
    /// <param name="i">
    /// The current read offset at the opening quote; advanced past the closing quote on success.
    /// </param>
    /// <param name="isFinal">Whether <paramref name="s" /> is the final block of input.</param>
    /// <param name="entryLine">The 1-based line number on which the entry begins.</param>
    /// <param name="value">When an entry is parsed, receives the literal string value.</param>
    /// <returns>
    /// <see cref="ReadResult.Entry" /> when the value was parsed; otherwise, <see cref="ReadResult.NeedMore" /> when
    /// more input is required.
    /// </returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated and <paramref name="isFinal" /> is <see langword="true" />, or when a line
    /// break occurs before the closing quote.
    /// </exception>
    private static ReadResult ParseSingleQuoted(ReadOnlySpan<char> s, ref int i, bool isFinal, int entryLine, out string value)
    {
        value = string.Empty;
        int contentStart = i + 1;

        for (int k = contentStart; k < s.Length; k++)
        {
            char c = s[k];

            if (c == '\'')
            {
                value = s[contentStart..k].ToString();
                i = k + 1;
                return ReadResult.Entry;
            }

            if (c is '\n' or '\r')
                DotEnv.ThrowUnterminatedSingleQuote(entryLine);
        }

        if (isFinal)
            DotEnv.ThrowUnterminatedSingleQuote(entryLine);

        return ReadResult.NeedMore;
    }

    /// <summary>
    /// Parses an unquoted value, trimming surrounding whitespace and honoring inline comments when enabled. Returns
    /// <see cref="ReadResult.NeedMore" /> when the end of the line has not yet been buffered.
    /// </summary>
    /// <param name="s">The pending input.</param>
    /// <param name="i">
    /// The current read offset at the start of the value; advanced to the end of the line on success.
    /// </param>
    /// <param name="isFinal">Whether <paramref name="s" /> is the final block of input.</param>
    /// <param name="options">The parse options that govern inline-comment handling.</param>
    /// <param name="value">When an entry is parsed, receives the trimmed value.</param>
    /// <returns>
    /// <see cref="ReadResult.Entry" /> when the value was parsed; otherwise, <see cref="ReadResult.NeedMore" /> when
    /// more input is required.
    /// </returns>
    private static ReadResult ParseUnquoted(ReadOnlySpan<char> s, ref int i, bool isFinal, DotEnvParseOptions options, out string value)
    {
        value = string.Empty;

        int end = i;
        while (end < s.Length && s[end] != '\n' && s[end] != '\r')
            end++;

        if (end >= s.Length && !isFinal)
            return ReadResult.NeedMore;

        ReadOnlySpan<char> raw = s[i..end];
        i = end;

        raw = raw.TrimStart();

        if (options.AllowInlineComments)
        {
            for (int k = 1; k < raw.Length; k++)
            {
                if (raw[k] == '#' && (raw[k - 1] == ' ' || raw[k - 1] == '\t'))
                {
                    raw = raw[..k];
                    break;
                }
            }
        }

        value = raw.TrimEnd().ToString();
        return ReadResult.Entry;
    }
}
