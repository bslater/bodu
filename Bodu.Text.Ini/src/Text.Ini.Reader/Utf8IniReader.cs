// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Serialization;

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
/// <para>
/// An input that arrives in blocks is read with the
/// <see cref="Utf8IniReader(ReadOnlySpan{byte}, bool, IniReaderState)" /> constructor: over a block that is not the
/// last, <see cref="Read" /> returns <see langword="false" /> rather than read a line that the block cuts short, and
/// the next block's reader resumes from <see cref="CurrentState" />, as <see cref="IniReaderState" /> describes.
/// </para>
/// </remarks>
public ref struct Utf8IniReader
{
    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The reader options.</summary>
    private readonly IniReaderOptions _options;

    /// <summary>Whether no further data follows <see cref="_data" />, so that its end is the end of the input.</summary>
    private readonly bool _isFinalBlock;

    /// <summary>The offset, in the whole input, of the first byte of <see cref="_data" />.</summary>
    private readonly long _offsetBase;

    /// <summary>Whether the block holds no more than the start of a byte-order mark, which more data must complete or refute.</summary>
    private readonly bool _byteOrderMarkPending;

    /// <summary>The read position of the next byte to consume.</summary>
    private int _position;

    /// <summary>The 1-based line number of the current read position.</summary>
    private int _line;

    /// <summary>The 1-based line number on which the current token begins.</summary>
    private int _tokenLine;

    /// <summary>The zero-based byte offset at which the current token begins.</summary>
    private int _tokenStart;

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

    /// <summary>The zero-based byte offset at which the pending string token begins.</summary>
    private int _pendingStart;

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
        : this(data, isFinalBlock: true, new IniReaderState(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8IniReader" /> struct over one block of an input, resuming from
    /// the state captured at the end of the previous block.
    /// </summary>
    /// <param name="data">
    /// The unconsumed bytes carried over from the previous block plus the newly arrived data.
    /// </param>
    /// <param name="isFinalBlock">
    /// <see langword="true" /> when no further data follows this block; <see langword="false" /> to make
    /// <see cref="Read" /> return <see langword="false" /> rather than read a line that the block cuts short.
    /// </param>
    /// <param name="state">
    /// The continuation state: a new <see cref="IniReaderState" /> for the first block, or the previous reader's
    /// <see cref="CurrentState" /> thereafter.
    /// </param>
    /// <remarks>
    /// <para>
    /// A section header, an entry or a comment is read only once the block holds the whole of its line: over a block
    /// that is not the last, once the line ending has arrived, and a carriage return that ends the block waits for the
    /// byte after it, which may be the line feed of a CRLF.
    /// </para>
    /// <para>
    /// A UTF-8 byte-order mark is skipped only at the start of the whole input; a first block that holds no more than
    /// the start of one is read once more data arrives.
    /// </para>
    /// </remarks>
    public Utf8IniReader(ReadOnlySpan<byte> data, bool isFinalBlock, IniReaderState state)
    {
        _data = data;
        _options = state.Options;
        _isFinalBlock = isFinalBlock;
        _offsetBase = state._bytesConsumed;
        _line = state._linesRead + 1;
        _tokenLine = _line;
        _tokenType = IniTokenType.None;

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first section or key. Only the
        // start of the whole input can hold one, and a block that holds no more than its first bytes, with more data to
        // come, cannot yet tell it from content.
        if (!state._pastStart)
        {
            if (data.StartsWith(Utf8LineCursor.ByteOrderMark))
                _position = Utf8LineCursor.ByteOrderMark.Length;
            else if (!isFinalBlock && Utf8LineCursor.ByteOrderMark.StartsWith(data))
                _byteOrderMarkPending = true;
        }

        _tokenStart = _position;

        // A value whose key was already reported resumes first.
        if (state._pendingString)
        {
            _pendingString = true;
            _pendingValue = state._pendingValue;
            _pendingLine = state._pendingLine;
            _pendingStart = (int)(state._pendingOffset - state._bytesConsumed);
        }
    }

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <value>The read position.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the resumable state to construct the next block's reader from.
    /// </summary>
    /// <value>
    /// The continuation state, capturing the options, the line counter, the offset of <see cref="BytesConsumed" /> in
    /// the whole input and, after a key, the value still to report.
    /// </value>
    public readonly IniReaderState CurrentState =>
        new(_options)
        {
            _linesRead = _line - 1,
            _bytesConsumed = _offsetBase + _position,
            _pastStart = !_byteOrderMarkPending,
            _pendingString = _pendingString,
            _pendingValue = _pendingString ? _pendingValue : null,
            _pendingLine = _pendingString ? _pendingLine : 0,
            _pendingOffset = _pendingString ? _offsetBase + _pendingStart : 0,
        };

    /// <summary>
    /// Gets a value indicating whether the supplied bytes hold the final block of the input.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the end of the bytes is the end of the input; <see langword="false" /> when
    /// <see cref="Read" /> instead returns <see langword="false" /> at a line the block cuts short, and the caller
    /// supplies more data.
    /// </value>
    public readonly bool IsFinalBlock => _isFinalBlock;

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
    /// Gets the zero-based byte offset at which the current token begins: the <c>[</c> of a section header, the comment
    /// marker of a comment, or the first byte of a key or value after the spaces and tabs around it.
    /// </summary>
    /// <value>The offset of the current token's first byte within the source.</value>
    /// <remarks>
    /// The document models report a section or key that their duplicate policies reject at this offset, on the line
    /// <see cref="LineNumber" /> gives.
    /// </remarks>
    internal readonly int TokenStartIndex => _tokenStart;

    /// <summary>
    /// Gets the offset, in the whole input, at which the current token begins.
    /// </summary>
    /// <value>
    /// The offset of the current token's first byte, counted from the start of the whole input rather than of the
    /// block.
    /// </value>
    internal readonly long TokenOffset => _offsetBase + _tokenStart;

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
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document, or, over a
    /// block that is not the last, when the block ends before the next line does.
    /// </returns>
    /// <exception cref="IniFormatException">Thrown when the bytes are not valid INI.</exception>
    /// <remarks>
    /// When the block ends before the next line does, the reader is left as it was: the current token is still the
    /// previous one, and <see cref="BytesConsumed" /> counts only the lines read.
    /// </remarks>
    public bool Read()
    {
        if (_pendingString)
        {
            _pendingString = false;
            _current = _pendingValue;
            _tokenType = IniTokenType.String;
            _tokenLine = _pendingLine;
            _tokenStart = _pendingStart;
            return true;
        }

        if (_byteOrderMarkPending)
            return false;

        while (true)
        {
            SkipSpacesAndTabs();

            if (_position >= _data.Length)
            {
                if (!_isFinalBlock)
                    return false;

                _tokenType = IniTokenType.None;
                _tokenLine = _line;
                _tokenStart = _position;
                _current = null;
                return false;
            }

            byte b = _data[_position];

            if (Utf8LineCursor.IsLineEnding(b))
            {
                if (EndsWithPendingCarriageReturn(_position))
                    return false;

                SkipLineEnding();
                continue;
            }

            // Every other line is read whole: over a block that is not the last, once its line ending has arrived.
            if (!_isFinalBlock && !HoldsWholeLine(_position))
                return false;

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
        int end = Utf8LineCursor.EndOfLine(_data, textStart);

        _position = end;

        if (_options.PreserveComments)
        {
            _current = Encoding.UTF8.GetString(_data[textStart..end]);
            _tokenType = IniTokenType.Comment;
            _tokenLine = _line;
            _tokenStart = textStart - 1;
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
        int headerStart = _position;
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
        _tokenStart = headerStart;
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

        _position = Utf8LineCursor.EndOfLineOr(_data, _position, (byte)'=');

        if (_position >= _data.Length || _data[_position] != (byte)'=')
            throw Error(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniMissingAssignment, entryLine), entryLine);

        (int trimmedKeyStart, int keyLength) = Trim(keyStart, _position);
        if (keyLength == 0)
            throw Error(IniResourceStrings.Format_Invalid_IniEmptyKey, entryLine);

        _position++; // consume '='

        int valueStart = _position;
        _position = Utf8LineCursor.EndOfLine(_data, _position);

        (int trimmedValueStart, int valueLength) = Trim(valueStart, _position);
        SkipLineEnding();

        _current = Encoding.UTF8.GetString(_data.Slice(trimmedKeyStart, keyLength));
        _tokenType = IniTokenType.PropertyName;
        _tokenLine = entryLine;
        _tokenStart = trimmedKeyStart;

        _pendingString = true;
        _pendingValue = Encoding.UTF8.GetString(_data.Slice(trimmedValueStart, valueLength));
        _pendingLine = entryLine;
        _pendingStart = trimmedValueStart;
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
    private void SkipToEndOfLine() =>
        _position = Utf8LineCursor.EndOfLine(_data, _position);

    /// <summary>
    /// Consumes the current line ending, updating the reader's logical line number.
    /// </summary>
    private void SkipLineEnding() =>
        _line += Utf8LineCursor.ConsumeLineEnding(_data, ref _position);

    /// <summary>
    /// Determines whether the block holds the whole of the line that a position is on, its line ending included.
    /// </summary>
    /// <param name="position">A position on the line, within the block.</param>
    /// <returns>
    /// <see langword="true" /> when the line's line ending lies within the block and can be consumed; otherwise
    /// <see langword="false" />.
    /// </returns>
    private readonly bool HoldsWholeLine(int position)
    {
        int end = Utf8LineCursor.EndOfLine(_data, position);
        return end < _data.Length && !EndsWithPendingCarriageReturn(end);
    }

    /// <summary>
    /// Determines whether the byte at a position is a carriage return that ends a block that is not the last, and so
    /// may be the first half of a CRLF whose line feed is still to come.
    /// </summary>
    /// <param name="position">The position of the line-ending byte, within the block.</param>
    /// <returns>
    /// <see langword="true" /> when the line ending cannot be consumed until more data arrives; otherwise
    /// <see langword="false" />.
    /// </returns>
    private readonly bool EndsWithPendingCarriageReturn(int position) =>
        !_isFinalBlock && position == _data.Length - 1 && _data[position] == (byte)'\r';

    /// <summary>
    /// Creates a parse exception with a source location.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw, at the current position's offset in the whole input.</returns>
    private readonly IniFormatException Error(string message, int line) =>
        new(message, line, _offsetBase + _position);
}
