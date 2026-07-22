// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Provides a high-performance, forward-only, source-order reader for INI bytes. The reader is a
/// <see langword="ref struct" />, so it lives on the stack and cannot be boxed or captured.
/// </summary>
/// <remarks>
/// <para>
/// A call to <see cref="Read" /> advances to the next token and reports it through <see cref="TokenType" />: a
/// <see cref="IniTokenType.SectionHeader" /> begins a section, each entry surfaces a
/// <see cref="IniTokenType.PropertyName" /> followed by a <see cref="IniTokenType.String" /> value, and comment lines
/// appear as <see cref="IniTokenType.Comment" /> tokens. Keys read before the first section header belong to the global
/// section.
/// </para>
/// <para>
/// The reader surfaces raw text in source order; it does not resolve duplicate sections/keys, apply case rules, or
/// strip inline comments. A value retains everything after the assignment to the end of the line.
/// </para>
/// </remarks>
public ref struct Utf8IniReader
{
    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The reader options.</summary>
    private readonly IniReaderOptions _options;

    /// <summary>The read position of the next byte to consume.</summary>
    private int _position;

    /// <summary>The 1-based line number of the current read position.</summary>
    private int _line;

    /// <summary>The kind of the current token.</summary>
    private IniTokenType _tokenType;

    /// <summary>The decoded text of the current token.</summary>
    private string? _current;

    /// <summary>Whether the next <see cref="Read" /> emits the string value of the entry whose key was just read.</summary>
    private bool _pendingString;

    /// <summary>The staged value text for the pending string token.</summary>
    private string? _pendingValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniReader" /> struct over the supplied bytes.
    /// </summary>
    /// <param name="data">The INI source bytes.</param>
    public Utf8IniReader(ReadOnlySpan<byte> data)
        : this(data, IniReaderOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniReader" /> struct over the supplied bytes using the supplied
    /// options.
    /// </summary>
    /// <param name="data">The INI source bytes.</param>
    /// <param name="options">The reader options.</param>
    public Utf8IniReader(ReadOnlySpan<byte> data, IniReaderOptions options)
    {
        _data = data;
        _options = options;

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first section or key.
        _position = data.StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
        _line = 1;
        _tokenType = IniTokenType.None;
    }

    /// <summary>
    /// Gets the UTF-8 byte-order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <value>The read position.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the 1-based line number at which the current token begins.
    /// </summary>
    /// <value>The current line number.</value>
    public readonly int LineNumber => _line;

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <value>The current token kind.</value>
    public readonly IniTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the decoded text of the current token — a section name, key name, string value, or comment.
    /// </summary>
    /// <returns>The token text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token has no text.</exception>
    public readonly string GetString() =>
        _current ?? throw new InvalidOperationException();

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="IniFormatException">Thrown when the bytes are not valid INI.</exception>
    public bool Read()
    {
        if (_pendingString)
        {
            _pendingString = false;
            _current = _pendingValue;
            _tokenType = IniTokenType.String;
            return true;
        }

        while (true)
        {
            SkipSpacesAndTabs();

            if (_position >= _data.Length)
            {
                _tokenType = IniTokenType.None;
                _current = null;
                return false;
            }

            byte b = _data[_position];

            if (b is (byte)'\r' or (byte)'\n')
            {
                SkipLineEnding();
                continue;
            }

            if (b == (byte)';' || (b == (byte)'#' && _options.AllowHashComments))
            {
                if (ReadComment())
                    return true;

                continue;
            }

            if (b == (byte)'[')
            {
                ReadSectionHeader();
                return true;
            }

            ReadEntry();
            return true;
        }
    }

    /// <summary>
    /// Reads a comment line, emitting a <see cref="IniTokenType.Comment" /> token when comments are preserved.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a comment token was emitted; <see langword="false" /> when it was skipped.
    /// </returns>
    private bool ReadComment()
    {
        int textStart = _position + 1;
        int end = textStart;
        while (end < _data.Length && _data[end] is not((byte)'\n' or (byte)'\r'))
            end++;

        _position = end;

        if (_options.PreserveComments)
        {
            _current = Encoding.UTF8.GetString(_data[textStart..end]);
            _tokenType = IniTokenType.Comment;
            SkipLineEnding();
            return true;
        }

        SkipLineEnding();
        return false;
    }

    /// <summary>
    /// Reads a section header (<c>[name]</c>) beginning at the cursor.
    /// </summary>
    /// <exception cref="IniFormatException">Thrown when the header is unterminated or the name is empty.</exception>
    private void ReadSectionHeader()
    {
        int headerLine = _line;
        _position++; // consume '['

        int start = _position;
        while (_position < _data.Length && _data[_position] is not((byte)']' or (byte)'\n' or (byte)'\r'))
            _position++;

        if (_position >= _data.Length || _data[_position] != (byte)']')
            throw Error(IniResourceStrings.Format_Invalid_IniUnterminatedSection, headerLine);

        int end = _position;
        _position++; // consume ']'

        (int nameStart, int nameLength) = Trim(start, end);
        if (nameLength == 0)
            throw Error(IniResourceStrings.Format_Invalid_IniEmptySectionName, headerLine);

        SkipToEndOfLine();
        SkipLineEnding();

        _current = Encoding.UTF8.GetString(_data.Slice(nameStart, nameLength));
        _tokenType = IniTokenType.SectionHeader;
    }

    /// <summary>
    /// Reads a <c>key=value</c> entry beginning at the cursor, emitting the key and staging the value.
    /// </summary>
    /// <exception cref="IniFormatException">Thrown when the entry has no assignment or an empty key.</exception>
    private void ReadEntry()
    {
        int entryLine = _line;
        int keyStart = _position;

        while (_position < _data.Length && _data[_position] is not((byte)'=' or (byte)'\n' or (byte)'\r'))
            _position++;

        if (_position >= _data.Length || _data[_position] != (byte)'=')
            throw Error(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniMissingAssignment, entryLine), entryLine);

        (int trimmedKeyStart, int keyLength) = Trim(keyStart, _position);
        if (keyLength == 0)
            throw Error(IniResourceStrings.Format_Invalid_IniEmptyKey, entryLine);

        _position++; // consume '='

        int valueStart = _position;
        while (_position < _data.Length && _data[_position] is not((byte)'\n' or (byte)'\r'))
            _position++;

        (int trimmedValueStart, int valueLength) = Trim(valueStart, _position);
        SkipLineEnding();

        _current = Encoding.UTF8.GetString(_data.Slice(trimmedKeyStart, keyLength));
        _tokenType = IniTokenType.PropertyName;

        _pendingString = true;
        _pendingValue = Encoding.UTF8.GetString(_data.Slice(trimmedValueStart, valueLength));
    }

    /// <summary>
    /// Trims leading and trailing spaces and tabs from the given source range.
    /// </summary>
    /// <param name="start">The inclusive start offset.</param>
    /// <param name="end">The exclusive end offset.</param>
    /// <returns>The trimmed start offset and length.</returns>
    private readonly (int Start, int Length) Trim(int start, int end)
    {
        while (start < end && _data[start] is (byte)' ' or (byte)'\t')
            start++;
        while (end > start && _data[end - 1] is (byte)' ' or (byte)'\t')
            end--;

        return (start, end - start);
    }

    /// <summary>
    /// Advances past space and tab characters on the current line.
    /// </summary>
    private void SkipSpacesAndTabs()
    {
        while (_position < _data.Length && _data[_position] is (byte)' ' or (byte)'\t')
            _position++;
    }

    /// <summary>
    /// Advances to the end of the current line without consuming the line terminator.
    /// </summary>
    private void SkipToEndOfLine()
    {
        while (_position < _data.Length && _data[_position] is not((byte)'\n' or (byte)'\r'))
            _position++;
    }

    /// <summary>
    /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_line" />.
    /// </summary>
    private void SkipLineEnding()
    {
        if (_position >= _data.Length)
            return;

        if (_data[_position] == (byte)'\r')
        {
            _position++;
            if (_position < _data.Length && _data[_position] == (byte)'\n')
                _position++;

            _line++;
        }
        else if (_data[_position] == (byte)'\n')
        {
            _position++;
            _line++;
        }
    }

    /// <summary>
    /// Creates a parse exception with a source location.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw.</returns>
    private readonly IniFormatException Error(string message, int line) =>
        new(message, line, _position);
}
