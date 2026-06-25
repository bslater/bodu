// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Lex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
            while (!IsBreakOrEnd(Peek()))
                _pos++;
        }

        if (!AtEnd)
            Advance();
    }

    /// <summary>
    /// Advances past blank lines and comment-only lines, leaving the cursor at the first non-space content byte.
    /// </summary>
    private void SkipBlankCommentLines()
    {
        while (!AtEnd)
        {
            while (_pos < _length && _source[_pos] is (byte)' ' or (byte)'\t')
                _pos++;

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

            return;
        }
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
    /// Skips leading <c>%YAML</c>/<c>%TAG</c> directive lines and a single leading <c>---</c> document-start marker.
    /// </summary>
    private void SkipDirectivesAndDocumentStart()
    {
        while (true)
        {
            SkipBlankCommentLines();
            if (!AtEnd && CurrentColumn() == 0 && Peek() == (byte)'%')
            {
                while (!IsBreakOrEnd(Peek()))
                    _pos++;

                if (!AtEnd)
                    Advance();

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

            if (Peek() == (byte)' ' && PeekAt(1) == (byte)'#')
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

            if (Peek() == (byte)' ' && PeekAt(1) == (byte)'#')
                break;

            _pos++;
            if (_source[_pos - 1] is not ((byte)' ' or (byte)'\t'))
                end = _pos;
        }

        return Utf8(start, end - start);
    }

    /// <summary>
    /// Reads an optional anchor (<c>&amp;name</c>) and tag (<c>!tag</c>) node-property prefix on the current line.
    /// </summary>
    /// <param name="tag">When the method returns, the captured tag, or <see langword="null" />.</param>
    /// <returns>The captured anchor name, or <see langword="null" />.</returns>
    private string? TryReadAnchorAndTag(out string? tag)
    {
        string? anchor = null;
        tag = null;

        while (true)
        {
            SkipSpaces();
            var c = Peek();
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
