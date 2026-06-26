// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Lex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Low-level lexing helpers and row-store factories for the YAML parser: byte cursor management, indentation and
/// line-break tracking, whitespace and comment skipping, document-boundary detection, and node construction.
/// </summary>
internal sealed partial class YamlParser
{
    private List<string> _strings = [];

    /// <summary>
    /// Gets the decoded-string side table referenced by string scalar rows.
    /// </summary>
    /// <value>The list of materialized string values, indexed by a string scalar's packed payload.</value>
    internal List<string> Strings => _strings;

    /// <summary>
    /// Gets a value indicating whether the cursor has reached the end of the source.
    /// </summary>
    /// <value><see langword="true" /> when no more bytes remain; otherwise <see langword="false" />.</value>
    private bool AtEnd => _pos >= _length;

    /// <summary>
    /// Returns the byte at the cursor, or zero at end of input.
    /// </summary>
    /// <returns>The current byte, or zero.</returns>
    private byte Peek() => _pos < _length ? _source[_pos] : (byte)0;

    /// <summary>
    /// Returns the byte at the given offset from the cursor, or zero when out of range.
    /// </summary>
    /// <param name="offset">The offset from the current position.</param>
    /// <returns>The byte at the offset, or zero.</returns>
    private byte PeekAt(int offset)
    {
        var i = _pos + offset;
        return i < _length ? _source[i] : (byte)0;
    }

    /// <summary>
    /// Gets the zero-based byte column of the cursor within the current line.
    /// </summary>
    /// <returns>The column index.</returns>
    private int CurrentColumn() => _pos - _lineStart;

    /// <summary>
    /// Advances the cursor by one byte, collapsing a CR/LF or CRLF line break and updating line tracking.
    /// </summary>
    private void Advance()
    {
        if (_pos >= _length)
            return;

        var b = _source[_pos];
        if (b == (byte)'\n')
        {
            _pos++;
            _line++;
            _lineStart = _pos;
        }
        else if (b == (byte)'\r')
        {
            _pos++;
            if (_pos < _length && _source[_pos] == (byte)'\n')
                _pos++;

            _line++;
            _lineStart = _pos;
        }
        else
        {
            _pos++;
        }
    }

    /// <summary>
    /// Determines whether a byte is a line break or the end-of-input sentinel.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte ends the current line.</returns>
    private static bool IsBreakOrEnd(byte b) => b is 0 or (byte)'\n' or (byte)'\r';

    /// <summary>
    /// Determines whether a byte is a space, a line break, or the end-of-input sentinel.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte separates or terminates a token.</returns>
    private static bool IsBlankOrBreakOrEnd(byte b) => b is (byte)' ' or (byte)'\t' || IsBreakOrEnd(b);

    /// <summary>
    /// Advances over inline spaces and tabs without crossing a line break.
    /// </summary>
    private void SkipSpaces()
    {
        while (_pos < _length)
        {
            var b = _source[_pos];
            if (b is (byte)' ' or (byte)'\t')
                _pos++;
            else
                break;
        }
    }

    /// <summary>
    /// Consumes trailing spaces and an optional comment on the current line, then the line break.
    /// </summary>
    private void SkipLineTrailing()
    {
        SkipSpaces();
        if (Peek() == (byte)'#')
        {
            RequireCommentSpacing();
            while (!IsBreakOrEnd(Peek()))
                _pos++;
        }
        else if (!IsBreakOrEnd(Peek()))
        {
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
        }

        if (!AtEnd)
            Advance();
    }

    /// <summary>
    /// Verifies that a comment indicator at the cursor is preceded by whitespace or begins the line.
    /// </summary>
    /// <exception cref="YamlFormatException">The comment is glued to preceding content.</exception>
    private void RequireCommentSpacing()
    {
        if (_pos > _lineStart && _source[_pos - 1] is not ((byte)' ' or (byte)'\t'))
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlCommentSpacing);
    }

    /// <summary>
    /// Advances past blank lines and comment-only lines, leaving the cursor at the first non-space content byte.
    /// </summary>
    /// <remarks>
    /// The leading whitespace of a structural content line is its indentation, which YAML requires to be spaces only.
    /// A tab encountered before content is therefore rejected; tabs on blank or comment-only lines are immaterial and
    /// pass through.
    /// </remarks>
    /// <exception cref="YamlFormatException">A tab is used to indent a structural content line.</exception>
    private void SkipBlankCommentLines()
    {
        while (!AtEnd)
        {
            var tabOffset = -1;
            while (_pos < _length && _source[_pos] is (byte)' ' or (byte)'\t')
            {
                if (_source[_pos] == (byte)'\t' && tabOffset < 0)
                    tabOffset = _pos;

                _pos++;
            }

            var c = Peek();
            if (c == (byte)'#')
            {
                while (!IsBreakOrEnd(Peek()))
                    _pos++;

                if (AtEnd)
                    return;

                Advance();
                continue;
            }

            if (IsBreakOrEnd(c))
            {
                if (AtEnd)
                    return;

                Advance();
                continue;
            }

            // Structural content begins here, so the leading whitespace is indentation and must not contain a tab.
            if (tabOffset >= 0)
                throw ErrorAt(tabOffset, YamlResourceStrings.Format_Invalid_YamlTabIndentation);

            return;
        }
    }

    /// <summary>
    /// Validates that the source is well-formed UTF-8 containing only YAML-printable characters, rejecting invalid byte
    /// sequences and non-printable control characters before structural parsing begins.
    /// </summary>
    /// <exception cref="YamlFormatException">
    /// The source contains invalid UTF-8 or a non-printable control character.
    /// </exception>
    private void ValidateSource()
    {
        var span = _source.AsSpan(0, _length);
        var offset = 0;
        while (offset < span.Length)
        {
            var status = Rune.DecodeFromUtf8(span[offset..], out var rune, out var consumed);
            if (status != OperationStatus.Done)
                throw ErrorAt(offset, YamlResourceStrings.Format_Invalid_YamlInvalidUtf8);

            if (!IsYamlPrintable(rune))
                throw ErrorAt(offset, YamlResourceStrings.Format_Invalid_YamlControlCharacter);

            offset += consumed;
        }
    }

    /// <summary>
    /// Determines whether a Unicode scalar value is permitted to appear literally in YAML source.
    /// </summary>
    /// <param name="rune">The decoded Unicode scalar value.</param>
    /// <returns><see langword="true" /> when the character is a YAML-printable character.</returns>
    /// <remarks>
    /// Implements the YAML 1.2 <c>c-printable</c> production. Surrogate code points cannot occur because a
    /// <see cref="Rune" /> only holds valid scalar values, so they are excluded implicitly.
    /// </remarks>
    private static bool IsYamlPrintable(Rune rune)
    {
        var v = rune.Value;
        return v is 0x09 or 0x0A or 0x0D or 0x85
            || (v >= 0x20 && v <= 0x7E)
            || (v >= 0xA0 && v <= 0xD7FF)
            || (v >= 0xE000 && v <= 0xFFFD)
            || (v >= 0x10000 && v <= 0x10FFFF);
    }

    /// <summary>
    /// Creates a format exception anchored at a specific byte offset, computing its line and column.
    /// </summary>
    /// <param name="offset">The zero-based byte offset of the error.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    private YamlFormatException ErrorAt(int offset, string message)
    {
        var line = 0;
        var lineStart = 0;
        var limit = Math.Min(offset, _length);
        for (var i = 0; i < limit; i++)
        {
            if (_source[i] == (byte)'\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return new YamlFormatException(message, line + 1, offset - lineStart + 1, offset);
    }

    /// <summary>
    /// Determines whether the cursor is at the end of the stream.
    /// </summary>
    /// <returns><see langword="true" /> when no content remains.</returns>
    private bool AtStreamEnd() => AtEnd;

    /// <summary>
    /// Determines whether the cursor is at a document-boundary marker (<c>---</c> or <c>...</c>) at column zero.
    /// </summary>
    /// <returns><see langword="true" /> when a document marker begins at the cursor.</returns>
    private bool AtDocumentBoundary()
    {
        if (CurrentColumn() != 0)
            return false;

        return Marker((byte)'-') || Marker((byte)'.');

        bool Marker(byte b) =>
            Peek() == b && PeekAt(1) == b && PeekAt(2) == b && IsBlankOrBreakOrEnd(PeekAt(3));
    }

    /// <summary>
    /// Skips a UTF-8 byte-order mark when present at the start of the source.
    /// </summary>
    private void SkipByteOrderMark()
    {
        if (_length >= 3 && _source[0] == 0xEF && _source[1] == 0xBB && _source[2] == 0xBF)
        {
            _pos = 3;
            _lineStart = 3;
        }
    }

    /// <summary>
    /// Validates and applies the document's <c>%YAML</c>/<c>%TAG</c> directives, then consumes a single leading
    /// <c>---</c> document-start marker.
    /// </summary>
    /// <exception cref="YamlFormatException">A directive is malformed or repeated.</exception>
    private void SkipDirectivesAndDocumentStart()
    {
        _tagHandles = null;
        var yamlDirectiveSeen = false;
        var directiveSeen = false;

        while (true)
        {
            SkipBlankCommentLines();
            if (!AtEnd && CurrentColumn() == 0 && Peek() == (byte)'%')
            {
                directiveSeen = true;
                ProcessDirective(ref yamlDirectiveSeen);
                continue;
            }

            break;
        }

        SkipBlankCommentLines();
        if (!AtEnd && CurrentColumn() == 0 && Peek() == (byte)'-' && AtDocumentBoundary())
        {
            Advance();
            Advance();
            Advance();
        }
        else if (directiveSeen)
        {
            // A directive must be followed by an explicit '---' document-start marker.
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);
        }
    }

    /// <summary>
    /// Parses and validates a single directive line beginning at the cursor, applying <c>%TAG</c> handles and checking
    /// <c>%YAML</c> version constraints. Unknown (reserved) directives are ignored per the specification.
    /// </summary>
    /// <param name="yamlDirectiveSeen">
    /// Tracks whether a <c>%YAML</c> directive has already appeared in the current document.
    /// </param>
    /// <exception cref="YamlFormatException">The directive is malformed or repeated.</exception>
    private void ProcessDirective(ref bool yamlDirectiveSeen)
    {
        var lineStart = _pos;
        Advance(); // '%'

        var nameStart = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()))
            _pos++;

        var name = Utf8(nameStart, _pos - nameStart);

        if (name == "YAML")
        {
            if (yamlDirectiveSeen)
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);

            yamlDirectiveSeen = true;
            var version = ReadDirectiveParameter();
            if (version is not ("1.1" or "1.2"))
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);
        }
        else if (name == "TAG")
        {
            var handle = ReadDirectiveParameter();
            var prefix = ReadDirectiveParameter();
            if (!IsValidTagHandle(handle) || prefix.Length == 0)
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);

            _tagHandles ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!_tagHandles.TryAdd(handle, prefix))
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);
        }

        // A known directive permits only a trailing comment after its parameters.
        if (name is "YAML" or "TAG")
        {
            SkipSpaces();
            if (!IsBreakOrEnd(Peek()) && Peek() != (byte)'#')
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);
        }

        // Consume the remainder of the directive line.
        while (!IsBreakOrEnd(Peek()))
            _pos++;

        if (!AtEnd)
            Advance();
    }

    /// <summary>
    /// Reads the next whitespace-delimited parameter token on the current directive line.
    /// </summary>
    /// <returns>The parameter text, or the empty string when none remains on the line.</returns>
    private string ReadDirectiveParameter()
    {
        SkipSpaces();
        var start = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()))
            _pos++;

        return Utf8(start, _pos - start);
    }

    /// <summary>
    /// Determines whether a tag handle is one of the three valid forms: the primary (<c>!</c>), secondary (<c>!!</c>),
    /// or a named handle (<c>!name!</c>).
    /// </summary>
    /// <param name="handle">The handle text.</param>
    /// <returns><see langword="true" /> when the handle is well-formed.</returns>
    private static bool IsValidTagHandle(string handle)
    {
        if (handle is "!" or "!!")
            return true;

        if (handle.Length < 3 || handle[0] != '!' || handle[^1] != '!')
            return false;

        for (var i = 1; i < handle.Length - 1; i++)
        {
            var c = handle[i];
            if (!char.IsLetterOrDigit(c) && c != '-')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Consumes the mapping value indicator <c>:</c> after a key.
    /// </summary>
    /// <exception cref="YamlFormatException">No value indicator follows the key.</exception>
    private void ExpectValueIndicator()
    {
        SkipSpaces();
        if (Peek() == (byte)':')
        {
            Advance();
            return;
        }

        throw Error(YamlResourceStrings.Format_Invalid_YamlExpectedValue);
    }

    /// <summary>
    /// Probes whether the current line begins a block mapping entry by scanning a candidate simple key followed by a
    /// value indicator, without advancing the cursor.
    /// </summary>
    /// <param name="indent">The candidate mapping indentation (unused by the scan but kept for symmetry).</param>
    /// <returns><see langword="true" /> when the line is a mapping entry; otherwise <see langword="false" />.</returns>
    private bool TryDetectBlockMapping(int indent)
    {
        _ = indent;
        var savePos = _pos;
        var saveLine = _line;
        var saveLineStart = _lineStart;
        try
        {
            return ScanSimpleKeyHasColon();
        }
        finally
        {
            _pos = savePos;
            _line = saveLine;
            _lineStart = saveLineStart;
        }
    }

    /// <summary>
    /// Scans a candidate simple key on the current line and reports whether a value indicator follows it.
    /// </summary>
    /// <returns><see langword="true" /> when a <c>:</c> value indicator terminates the key on this line.</returns>
    private bool ScanSimpleKeyHasColon()
    {
        var c = Peek();

        // An explicit key indicator '?' introduces a mapping entry even without a colon on the same line.
        if (c == (byte)'?' && IsBlankOrBreakOrEnd(PeekAt(1)))
            return true;

        if (c == (byte)'"' || c == (byte)'\'')
        {
            if (!SkipQuotedOnLine(c))
                return false;

            SkipSpaces();
            return Peek() == (byte)':' && IsBlankOrBreakOrEnd(PeekAt(1));
        }

        while (!IsBreakOrEnd(Peek()))
        {
            if (Peek() == (byte)':' && IsBlankOrBreakOrEnd(PeekAt(1)))
                return true;

            if (Peek() is (byte)' ' or (byte)'\t' && PeekAt(1) == (byte)'#')
                return false;

            _pos++;
        }

        return false;
    }

    /// <summary>
    /// Skips a single-line quoted scalar during probing, returning whether it terminated on the line.
    /// </summary>
    /// <param name="quote">The quote byte, either a single or double quote.</param>
    /// <returns><see langword="true" /> when a matching closing quote was found on the line.</returns>
    private bool SkipQuotedOnLine(byte quote)
    {
        _pos++; // opening quote
        while (!IsBreakOrEnd(Peek()))
        {
            var b = Peek();
            if (quote == (byte)'\'' && b == (byte)'\'')
            {
                if (PeekAt(1) == (byte)'\'')
                {
                    _pos += 2;
                    continue;
                }

                _pos++;
                return true;
            }

            if (quote == (byte)'"' && b == (byte)'\\')
            {
                _pos += 2;
                continue;
            }

            if (quote == (byte)'"' && b == (byte)'"')
            {
                _pos++;
                return true;
            }

            _pos++;
        }

        return false;
    }

    /// <summary>
    /// Reads a simple mapping key (plain or single-line quoted), leaving the cursor at the value indicator.
    /// </summary>
    /// <returns>The decoded key text.</returns>
    private string ReadSimpleKey()
    {
        // A simple key may carry leading anchor and tag node properties; they are consumed so the key text is clean.
        if (Peek() is (byte)'&' or (byte)'!')
        {
            TryReadAnchorAndTag(out _);
            SkipSpaces();
        }

        var c = Peek();
        if (c == (byte)'"')
            return ReadDoubleQuoted();

        if (c == (byte)'\'')
            return ReadSingleQuoted();

        var start = _pos;
        var end = _pos;
        while (!IsBreakOrEnd(Peek()))
        {
            if (Peek() == (byte)':' && IsBlankOrBreakOrEnd(PeekAt(1)))
                break;

            if (Peek() is (byte)' ' or (byte)'\t' && PeekAt(1) == (byte)'#')
                break;

            _pos++;
            if (_source[_pos - 1] is not ((byte)' ' or (byte)'\t'))
                end = _pos;
        }

        return Utf8(start, end - start);
    }

    /// <summary>
    /// Reads an optional anchor (<c>&amp;name</c>) and tag (<c>!tag</c>) node-property prefix.
    /// </summary>
    /// <param name="tag">When the method returns, the captured tag, or <see langword="null" />.</param>
    /// <param name="crossLines">
    /// Whether the node properties may be separated from each other and the node by line breaks (block context).
    /// </param>
    /// <returns>The captured anchor name, or <see langword="null" />.</returns>
    private string? TryReadAnchorAndTag(out string? tag, bool crossLines = false)
    {
        string? anchor = null;
        tag = null;

        while (true)
        {
            if (crossLines)
                SkipBlankCommentLines();
            else
                SkipSpaces();

            var c = Peek();

            // In block context, an anchor or tag that begins a mapping-entry line is the entry key's property, not a
            // property of the node being read.
            if (crossLines && c is (byte)'&' or (byte)'!' && TryDetectBlockMapping(0))
                break;

            if (c == (byte)'&' && anchor is null)
            {
                anchor = ReadName(1);
                continue;
            }

            if (c == (byte)'!' && tag is null)
            {
                tag = ReadTag();
                continue;
            }

            break;
        }

        return anchor;
    }

    /// <summary>
    /// Parses an alias node (<c>*name</c>) and appends an unresolved alias row.
    /// </summary>
    /// <returns>The row index of the alias.</returns>
    private int ParseAlias()
    {
        var offset = _pos;
        var name = ReadName(1);
        var row = new YamlReaderRow
        {
            Kind = YamlReaderNodeKind.Alias,
            ValueKind = YamlValueKind.None,
            ScalarStyle = YamlScalarStyle.Plain,
            Key = null,
            Anchor = null,
            Tag = name,
            Offset = offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
            AliasTarget = -1,
            ChildCount = 0,
            Depth = 0,
            Flags = YamlReaderRowFlags.None,
        };
        _rows.Add(row);
        return _rows.Count - 1;
    }

    /// <summary>
    /// Reads an anchor or alias name beginning after the given indicator length.
    /// </summary>
    /// <param name="skip">The number of indicator bytes to consume before the name.</param>
    /// <returns>The decoded name.</returns>
    private string ReadName(int skip)
    {
        for (var i = 0; i < skip; i++)
            Advance();

        var start = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()) && Peek() is not ((byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}'))
            _pos++;

        return Utf8(start, _pos - start);
    }

    /// <summary>
    /// Reads a tag node property (verbatim or shorthand) up to the next separator.
    /// </summary>
    /// <returns>The captured tag text.</returns>
    private string ReadTag()
    {
        var start = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()) && Peek() is not ((byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}'))
            _pos++;

        return Utf8(start, _pos - start);
    }

    /// <summary>
    /// Appends a child row to a container, maintaining the sibling chain and child count.
    /// </summary>
    /// <param name="parent">The container row index.</param>
    /// <param name="child">The child row index.</param>
    private void AppendChild(int parent, int child)
    {
        var p = _rows[parent];
        if (p.FirstChild < 0)
        {
            p.FirstChild = child;
            p.LastChild = child;
        }
        else
        {
            var last = _rows[p.LastChild];
            last.NextSibling = child;
            _rows[p.LastChild] = last;
            p.LastChild = child;
        }

        p.ChildCount++;
        _rows[parent] = p;
    }

    /// <summary>
    /// Creates a scalar row with a non-string payload (null, boolean, integer, or float).
    /// </summary>
    /// <param name="kind">The resolved value kind.</param>
    /// <param name="bits">The packed payload.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="offset">The byte offset where the scalar begins.</param>
    /// <param name="anchor">The anchor name, or <see langword="null" />.</param>
    /// <param name="tag">The tag, or <see langword="null" />.</param>
    /// <returns>The row index of the scalar.</returns>
    private int NewScalar(YamlValueKind kind, long bits, YamlScalarStyle style, int offset, string? anchor, string? tag)
    {
        var row = new YamlReaderRow
        {
            Kind = YamlReaderNodeKind.Scalar,
            ValueKind = kind,
            ScalarStyle = style,
            ScalarBits = bits,
            Key = null,
            Anchor = anchor,
            Tag = tag,
            Offset = offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
            AliasTarget = -1,
            ChildCount = 0,
            Depth = 0,
            Flags = anchor is null ? YamlReaderRowFlags.None : YamlReaderRowFlags.Anchored,
        };
        _rows.Add(row);
        return _rows.Count - 1;
    }

    /// <summary>
    /// Creates a string scalar row, storing the decoded value in the side table.
    /// </summary>
    /// <param name="value">The decoded string value.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="offset">The byte offset where the scalar begins.</param>
    /// <param name="anchor">The anchor name, or <see langword="null" />.</param>
    /// <param name="tag">The tag, or <see langword="null" />.</param>
    /// <returns>The row index of the scalar.</returns>
    private int NewString(string value, YamlScalarStyle style, int offset, string? anchor, string? tag)
    {
        var index = _strings.Count;
        _strings.Add(value);
        return NewScalar(YamlValueKind.String, index, style, offset, anchor, tag);
    }

    /// <summary>
    /// Creates a container row (sequence or mapping).
    /// </summary>
    /// <param name="kind">The container kind.</param>
    /// <param name="offset">The byte offset where the container begins.</param>
    /// <param name="anchor">The anchor name, or <see langword="null" />.</param>
    /// <param name="tag">The tag, or <see langword="null" />.</param>
    /// <returns>The row index of the container.</returns>
    private int NewContainer(YamlReaderNodeKind kind, int offset, string? anchor, string? tag)
    {
        var row = new YamlReaderRow
        {
            Kind = kind,
            ValueKind = YamlValueKind.None,
            ScalarStyle = YamlScalarStyle.Any,
            Key = null,
            Anchor = anchor,
            Tag = tag,
            Offset = offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
            AliasTarget = -1,
            ChildCount = 0,
            Depth = 0,
            Flags = anchor is null ? YamlReaderRowFlags.None : YamlReaderRowFlags.Anchored,
        };
        _rows.Add(row);
        return _rows.Count - 1;
    }

    /// <summary>
    /// Materializes the decoded string value of a string scalar row.
    /// </summary>
    /// <param name="row">The scalar row.</param>
    /// <returns>The decoded string.</returns>
    private string MaterializeString(YamlReaderRow row) => _strings[(int)row.ScalarBits];

    /// <summary>
    /// Decodes a UTF-8 span from the source into a string.
    /// </summary>
    /// <param name="start">The start byte offset.</param>
    /// <param name="length">The byte length.</param>
    /// <returns>The decoded string.</returns>
    private string Utf8(int start, int length) =>
        length <= 0 ? string.Empty : Encoding.UTF8.GetString(_source, start, length);
}
