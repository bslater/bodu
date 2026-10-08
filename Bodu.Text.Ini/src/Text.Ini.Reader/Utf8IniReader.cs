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
/// strip inline comments. The spaces and tabs around a key and around a value are trimmed; everything else in a value,
/// to the end of the line, is literal, inline comment markers included.
/// </para>
/// <para>
/// A section name runs from the <c>[</c> to the first <c>]</c> on the line that only whitespace or a comment follows,
/// so a name may contain <c>]</c>: <c>[foo]bar]</c> names the section <c>foo]bar</c>. A comment starts with <c>;</c>
/// or, unless <see cref="IniReaderOptions.DisallowHashComments" /> is set, <c>#</c>. A comment after a header is
/// skipped rather than reported as a <see cref="IniTokenType.Comment" /> token, and any other text after the header
/// throws <see cref="IniFormatException" />.
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

    /// <summary>The 1-based line number on which the current token begins.</summary>
    private int _tokenLine;

    /// <summary>The kind of the current token.</summary>
    private IniTokenType _tokenType;

    /// <summary>The decoded text of the current token.</summary>
    private string? _current;

    /// <summary>Whether the next <see cref="Read" /> emits the string value of the entry whose key was just read.</summary>
    private bool _pendingString;

    /// <summary>The staged value text for the pending string token.</summary>
    private string? _pendingValue;

    /// <summary>The 1-based line number on which the pending string token begins.</summary>
    private int _pendingLine;

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
        _tokenLine = 1;
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
    /// <value>The line on which the current token begins.</value>
    /// <remarks>
    /// Once <see cref="Read" /> returns <see langword="false" />, the line is the one at the end of the input. A line
    /// ends at a LF, a CR LF pair or a lone CR.
    /// </remarks>
    public readonly int LineNumber => _tokenLine;

    /// <summary>
    /// Gets the 1-based line number of the read position, which is past the line ending of the token just read.
    /// </summary>
    /// <value>The line of the next byte to read.</value>
    /// <remarks>
    /// The document models report a section or key that their duplicate policies reject at this line, beside
    /// <see cref="BytesConsumed" />, so that the line and the offset of the error describe the same position.
    /// </remarks>
    internal readonly int PositionLineNumber => _line;

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <value>The current token kind.</value>
    public readonly IniTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the decoded text of the current token - a section name, key name, string value, or comment.
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
            _tokenLine = _pendingLine;
            return true;
        }

        while (true)
        {
            SkipSpacesAndTabs();

            if (_position >= _data.Length)
            {
                _tokenType = IniTokenType.None;
                _tokenLine = _line;
                _current = null;
                return false;
            }

            byte b = _data[_position];

            if (b is (byte)'\r' or (byte)'\n')
            {
                SkipLineEnding();
                continue;
            }

            if (IsCommentStart(b))
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
        while (end < _data.Length && _data[end] is not ((byte)'\n' or (byte)'\r'))
            end++;

        _position = end;

        if (_options.PreserveComments)
        {
            _current = Encoding.UTF8.GetString(_data[textStart..end]);
            _tokenType = IniTokenType.Comment;
            _tokenLine = _line;
            SkipLineEnding();
            return true;
        }

        SkipLineEnding();
        return false;
    }

    /// <summary>
    /// Reads a section header (<c>[name]</c>) beginning at the cursor.
    /// </summary>
    /// <remarks>
    /// The name runs to the first <c>]</c> on the line that only whitespace or a comment follows, so it may itself
    /// contain <c>]</c>. The whitespace or comment after that <c>]</c> is skipped.
    /// </remarks>
    /// <exception cref="IniFormatException">
    /// Thrown when the line holds no <c>]</c>, when text that is neither whitespace nor a comment follows every
    /// <c>]</c> on it, or when the name is empty.
    /// </exception>
    private void ReadSectionHeader()
    {
        int headerLine = _line;
        _position++; // consume '['

        int start = _position;
        SkipToEndOfLine();
        int lineEnd = _position;

        int end = FindHeaderEnd(start, lineEnd, out int trailingText);
        if (end < 0)
        {
            if (trailingText < 0)
                throw Error(IniResourceStrings.Format_Invalid_IniUnterminatedSection, headerLine);

            _position = trailingText;
            throw Error(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniSectionTrailingText, headerLine), headerLine);
        }

        _position = end + 1; // consume through the ']' that ends the header

        (int nameStart, int nameLength) = Trim(start, end);
        if (nameLength == 0)
            throw Error(IniResourceStrings.Format_Invalid_IniEmptySectionName, headerLine);

        _position = lineEnd; // skip the whitespace or comment after the header
        SkipLineEnding();

        _current = Encoding.UTF8.GetString(_data.Slice(nameStart, nameLength));
        _tokenType = IniTokenType.SectionHeader;
        _tokenLine = headerLine;
    }

    /// <summary>
    /// Finds the <c>]</c> that ends a section header: the first on the line that only whitespace or a comment follows.
    /// </summary>
    /// <param name="start">The offset just past the opening <c>[</c>.</param>
    /// <param name="lineEnd">The offset of the line terminator, or the length of the data.</param>
    /// <param name="trailingText">
    /// When the method returns -1, the offset of the first byte that is not whitespace after the last <c>]</c> on the
    /// line, or -1 when the line holds no <c>]</c>.
    /// </param>
    /// <returns>The offset of the <c>]</c> that ends the header, or -1 when no <c>]</c> on the line does.</returns>
    private readonly int FindHeaderEnd(int start, int lineEnd, out int trailingText)
    {
        trailingText = -1;
        int index = start;

        while (index < lineEnd)
        {
            if (_data[index] != (byte)']')
            {
                index++;
                continue;
            }

            // What follows this ']' after any whitespace decides: the end of the line or a comment ends the header
            // here, and anything else is part of the name, or the text after the header when no later ']' ends it. The
            // scan resumes past the whitespace, which holds no ']', so no byte is examined more than twice.
            int next = index + 1;
            while (next < lineEnd && _data[next] is (byte)' ' or (byte)'\t')
                next++;

            if (next == lineEnd || IsCommentStart(_data[next]))
                return index;

            trailingText = next;
            index = next;
        }

        return -1;
    }

    /// <summary>
    /// Determines whether a byte starts a comment: <c>;</c> always, and <c>#</c> unless the options disallow it.
    /// </summary>
    /// <param name="value">The byte to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> starts a comment; otherwise <see langword="false" />.
    /// </returns>
    private readonly bool IsCommentStart(byte value) =>
        value == (byte)';' || (value == (byte)'#' && _options.AllowHashComments);

    /// <summary>
    /// Reads a <c>key=value</c> entry beginning at the cursor, emitting the key and staging the value.
    /// </summary>
    /// <exception cref="IniFormatException">Thrown when the entry has no assignment or an empty key.</exception>
    private void ReadEntry()
    {
        int entryLine = _line;
        int keyStart = _position;

        while (_position < _data.Length && _data[_position] is not ((byte)'=' or (byte)'\n' or (byte)'\r'))
            _position++;

        if (_position >= _data.Length || _data[_position] != (byte)'=')
            throw Error(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniMissingAssignment, entryLine), entryLine);

        (int trimmedKeyStart, int keyLength) = Trim(keyStart, _position);
        if (keyLength == 0)
            throw Error(IniResourceStrings.Format_Invalid_IniEmptyKey, entryLine);

        _position++; // consume '='

        int valueStart = _position;
        while (_position < _data.Length && _data[_position] is not ((byte)'\n' or (byte)'\r'))
            _position++;

        (int trimmedValueStart, int valueLength) = Trim(valueStart, _position);
        SkipLineEnding();

        _current = Encoding.UTF8.GetString(_data.Slice(trimmedKeyStart, keyLength));
        _tokenType = IniTokenType.PropertyName;
        _tokenLine = entryLine;

        _pendingString = true;
        _pendingValue = Encoding.UTF8.GetString(_data.Slice(trimmedValueStart, valueLength));
        _pendingLine = entryLine;
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
        while (_position < _data.Length && _data[_position] is not ((byte)'\n' or (byte)'\r'))
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
