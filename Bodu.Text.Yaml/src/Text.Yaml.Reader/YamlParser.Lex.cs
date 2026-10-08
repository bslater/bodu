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
    /// <summary>The decoded-string side table referenced by string scalar rows.</summary>
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
        int i = _pos + offset;
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

        byte b = _source[_pos];
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
            byte b = _source[_pos];
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
    /// The leading whitespace of a structural content line is its indentation, which YAML requires to be spaces only. A
    /// tab encountered before content is therefore rejected; tabs on blank or comment-only lines are immaterial and
    /// pass through.
    /// </remarks>
    /// <exception cref="YamlFormatException">A tab is used to indent a structural content line.</exception>
    private void SkipBlankCommentLines()
    {
        while (!AtEnd)
        {
            int tabOffset = -1;
            while (_pos < _length && _source[_pos] is (byte)' ' or (byte)'\t')
            {
                if (_source[_pos] == (byte)'\t' && tabOffset < 0)
                    tabOffset = _pos;

                _pos++;
            }

            byte c = Peek();
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
        Span<byte> span = _source.AsSpan(0, _length);
        int offset = 0;
        while (offset < span.Length)
        {
            OperationStatus status = Rune.DecodeFromUtf8(span[offset..], out Rune rune, out int consumed);
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
        int v = rune.Value;
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
        int line = 0;
        int lineStart = 0;
        int limit = Math.Min(offset, _length);
        for (int i = 0; i < limit; i++)
        {
            byte b = _source[i];
            if (b == (byte)'\n')
            {
                line++;
                lineStart = i + 1;
            }
            else if (b == (byte)'\r' && (i + 1 >= _length || _source[i + 1] != (byte)'\n'))
            {
                // A lone CR is a line break (Advance collapses CRLF, so only count the CR when no LF follows).
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
    /// Determines whether the cursor is at a document-boundary marker (<c>---</c> or <c>...</c>) at column zero, or at
    /// a byte-order mark at column zero that opens the prefix of a following document.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a document marker, or the prefix of the next document, begins at the cursor.
    /// </returns>
    private bool AtDocumentBoundary()
    {
        if (CurrentColumn() != 0)
            return false;

        return IsMarkerAt(_pos, (byte)'-') || IsMarkerAt(_pos, (byte)'.') || AtByteOrderMarkBeforeDocumentStart();
    }

    /// <summary>
    /// Determines whether a three-character document marker (<c>---</c> or <c>...</c>) followed by a separator begins
    /// at a byte offset.
    /// </summary>
    /// <param name="offset">The byte offset to test.</param>
    /// <param name="marker">The marker character, <c>-</c> or <c>.</c>.</param>
    /// <returns><see langword="true" /> when the marker begins at <paramref name="offset" />.</returns>
    private bool IsMarkerAt(int offset, byte marker) =>
        ByteAt(offset) == marker && ByteAt(offset + 1) == marker && ByteAt(offset + 2) == marker
        && IsBlankOrBreakOrEnd(ByteAt(offset + 3));

    /// <summary>
    /// Returns the byte at an absolute offset, or zero when the offset is past the end of the source.
    /// </summary>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The byte at the offset, or zero.</returns>
    private byte ByteAt(int offset) => offset < _length ? _source[offset] : (byte)0;

    /// <summary>
    /// Determines whether a UTF-8 byte-order mark begins at a byte offset.
    /// </summary>
    /// <param name="offset">The byte offset to test.</param>
    /// <returns><see langword="true" /> when the three bytes at <paramref name="offset" /> encode U+FEFF.</returns>
    private bool IsByteOrderMarkAt(int offset) =>
        ByteAt(offset) == 0xEF && ByteAt(offset + 1) == 0xBB && ByteAt(offset + 2) == 0xBF;

    /// <summary>
    /// Determines whether a byte-order mark at the cursor opens the prefix of a following document: after it come only
    /// blank lines, comment lines, and further byte-order marks at the start of a line, and then a <c>---</c> marker.
    /// </summary>
    /// <returns><see langword="true" /> when the byte-order mark belongs to the next document.</returns>
    /// <remarks>
    /// A byte-order mark may begin any document in a stream but cannot appear inside one, so a mark at the start of a
    /// line ends the current document when the next document's <c>---</c> follows it.
    /// </remarks>
    private bool AtByteOrderMarkBeforeDocumentStart()
    {
        if (!IsByteOrderMarkAt(_pos))
            return false;

        int p = _pos;
        while (true)
        {
            // Here p is at the start of a line; marks there are part of the prefix, and the marker may follow them.
            while (IsByteOrderMarkAt(p))
                p += 3;

            if (IsMarkerAt(p, (byte)'-'))
                return true;

            while (ByteAt(p) is (byte)' ' or (byte)'\t')
                p++;

            if (ByteAt(p) == (byte)'#')
            {
                while (!IsBreakOrEnd(ByteAt(p)))
                    p++;
            }

            if (p >= _length || ByteAt(p) is not ((byte)'\n' or (byte)'\r'))
                return false;

            p += ByteAt(p) == (byte)'\r' && ByteAt(p + 1) == (byte)'\n' ? 2 : 1;
        }
    }

    /// <summary>
    /// Advances past the prefix that may precede a document: blank lines, comment lines, and a byte-order mark at the
    /// start of any of them, leaving the cursor at the document's first directive, marker, or content byte.
    /// </summary>
    /// <remarks>
    /// A byte-order mark may begin any document in a stream, not only the first, and may also begin the first content
    /// line after a document's <c>---</c>. Columns on the rest of the mark's line count from after the mark, so a
    /// marker or key that follows it is at column zero.
    /// </remarks>
    private void SkipDocumentPrefix()
    {
        while (true)
        {
            SkipBlankCommentLines();
            if (CurrentColumn() != 0 || !IsByteOrderMarkAt(_pos))
                return;

            _pos += 3;
            _lineStart = _pos;
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
        _documentStartConsumed = false;

        // Each document starts from the requested baseline version; a %YAML directive then overrides it for this
        // document only (the directive is honored over the configured option).
        _version = _optionVersion;
        bool yamlDirectiveSeen = false;
        bool directiveSeen = false;

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
            _documentStartConsumed = true;

            // A block mapping cannot begin on the document-start line.
            SkipSpaces();
            if (!IsBreakOrEnd(Peek()) && Peek() != (byte)'#' && TryDetectBlockMapping(0))
                throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
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
        int lineStart = _pos;
        Advance(); // '%'

        int nameStart = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()))
            _pos++;

        string name = Utf8(nameStart, _pos - nameStart);

        if (name == "YAML")
        {
            if (yamlDirectiveSeen)
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);

            yamlDirectiveSeen = true;
            string version = ReadDirectiveParameter();
            if (version is not ("1.1" or "1.2"))
                throw ErrorAt(lineStart, YamlResourceStrings.Format_Invalid_YamlInvalidDirective);

            // Honor the document's declared version for scalar resolution, overriding the configured option.
            _version = version == "1.1" ? YamlSpecVersion.V1_1 : YamlSpecVersion.V1_2;
        }
        else if (name == "TAG")
        {
            string handle = ReadDirectiveParameter();
            string prefix = ReadDirectiveParameter();
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
        int start = _pos;
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

        for (int i = 1; i < handle.Length - 1; i++)
        {
            char c = handle[i];
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
        int savePos = _pos;
        int saveLine = _line;
        int saveLineStart = _lineStart;
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
        byte c = Peek();

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
            byte b = Peek();
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
    /// <remarks>
    /// A key decodes to text only, so a key's node properties are not kept with it; an anchored key is instead kept as
    /// a scalar row outside the tree, which an alias can refer to.
    /// </remarks>
    /// <exception cref="YamlFormatException">
    /// The key is an alias or a flow collection, or a plain key begins with an indicator that cannot begin a plain
    /// scalar.
    /// </exception>
    private string ReadSimpleKey()
    {
        string? anchor = null;
        string? tag = null;
        if (Peek() is (byte)'&' or (byte)'!')
        {
            anchor = TryReadAnchorAndTag(out tag);
            SkipSpaces();
        }

        int offset = _pos;
        byte c = Peek();

        // An alias cannot serve as an implicit mapping key.
        if (c == (byte)'*')
            throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
        if (c == (byte)'"' || c == (byte)'\'')
        {
            int startLine = _line;
            string quoted = c == (byte)'"' ? ReadDoubleQuoted(-1) : ReadSingleQuoted(-1);

            // An implicit key must occupy a single line.
            if (_line != startLine)
                throw Error(YamlResourceStrings.Format_Invalid_YamlMultilineImplicitKey);

            if (anchor is not null)
                _ = NewString(quoted, c == (byte)'"' ? YamlScalarStyle.DoubleQuoted : YamlScalarStyle.SingleQuoted, offset, anchor, tag);

            return quoted;
        }

        // A flow collection in implicit-key position is a sequence or mapping key, which the profile rejects rather than
        // reading its brackets as the text of a scalar key.
        if (c is (byte)'[' or (byte)'{')
            throw Error(YamlResourceStrings.Format_Invalid_YamlComplexKeyUnsupported);

        // A plain key is a plain scalar, so it cannot begin with an indicator (production 126, ns-plain-first); a value
        // indicator at the start of the entry is an empty key instead.
        if (!(c == (byte)':' && IsBlankOrBreakOrEnd(PeekAt(1))) && !CanBeginPlainScalar(flow: false))
            throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        int start = _pos;
        int end = _pos;
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

        if (anchor is not null)
            _ = NewPlainScalar(start, end, anchor, tag);

        return Utf8(start, end - start);
    }

    /// <summary>
    /// Reads an optional anchor (<c>&amp;name</c>) and tag (<c>!tag</c>) node-property prefix.
    /// </summary>
    /// <param name="tag">When the method returns, the captured tag, or <see langword="null" />.</param>
    /// <param name="crossLines">
    /// Whether the node properties may be separated from each other and the node by line breaks (block context).
    /// </param>
    /// <param name="minIndent">
    /// The minimum column of a line the properties continue onto, when <paramref name="crossLines" /> is
    /// <see langword="true" />; a less-indented line belongs to an enclosing node.
    /// </param>
    /// <param name="flow">
    /// Whether the node is in flow context, where a comma or closing bracket directly after a property ends the node.
    /// </param>
    /// <returns>The captured anchor name, or <see langword="null" />.</returns>
    /// <exception cref="YamlFormatException">
    /// A property is repeated, has an empty name, or is not separated from the content that follows it.
    /// </exception>
    private string? TryReadAnchorAndTag(out string? tag, bool crossLines = false, int minIndent = 0, bool flow = false)
    {
        string? anchor = null;
        tag = null;

        while (true)
        {
            if (crossLines)
            {
                // Separation on the current line may hold tabs; only a following line's indentation may not.
                SkipSpaces();
                if (IsBreakOrEnd(Peek()) || Peek() == (byte)'#')
                    SkipBlankCommentLines();

                if (CurrentColumn() < minIndent)
                    break;
            }
            else
            {
                SkipSpaces();
            }

            byte c = Peek();

            // In block context, properties followed on their line by an implicit key are the key's properties, not
            // those of the node being read.
            if (crossLines && c is (byte)'&' or (byte)'!' && PropertiesPrecedeImplicitKey())
                break;

            // A node carries at most one anchor and one tag.
            if (c == (byte)'&')
            {
                if (anchor is not null)
                    throw Error(YamlResourceStrings.Format_Invalid_YamlMultipleAnchors);

                anchor = ReadName(1);
                RequirePropertySeparation(flow);
                _lastPropertyLine = _line;
                continue;
            }

            if (c == (byte)'!')
            {
                if (tag is not null)
                    throw Error(YamlResourceStrings.Format_Invalid_YamlMultipleTags);

                tag = ReadTag();
                RequirePropertySeparation(flow);
                _lastPropertyLine = _line;
                continue;
            }

            break;
        }

        return anchor;
    }

    /// <summary>
    /// Determines whether the node properties at the cursor are followed on their line by an implicit mapping key,
    /// which makes them the key's properties rather than those of the node being read.
    /// </summary>
    /// <returns><see langword="true" /> when an implicit key follows the properties on their line.</returns>
    /// <remarks>
    /// The properties are skipped token by token, so an anchor name that ends in <c>:</c> keeps its colon, and only the
    /// token after them decides. A <c>: </c> inside a flow collection there does not make the line a mapping entry; a
    /// value indicator after the whole collection, on the same line, makes the collection an implicit key. An alias or
    /// a block scalar there is the node the properties apply to.
    /// </remarks>
    private bool PropertiesPrecedeImplicitKey()
    {
        int savePos = _pos;
        int saveLine = _line;
        int saveLineStart = _lineStart;
        try
        {
            while (Peek() is (byte)'&' or (byte)'!')
            {
                if (Peek() == (byte)'&')
                    _ = ReadName(1);
                else
                    _ = ReadTag();

                SkipSpaces();
            }

            byte c = Peek();
            if (c is (byte)'[' or (byte)'{')
            {
                if (!SkipFlowCollectionOnLine())
                    return false;

                SkipSpaces();
                return Peek() == (byte)':' && IsBlankOrBreakOrEnd(PeekAt(1));
            }

            if (IsBreakOrEnd(c) || c is (byte)'#' or (byte)'*' or (byte)'|' or (byte)'>')
                return false;

            // A block sequence entry or an explicit key indicator is not an implicit key.
            if (c is (byte)'-' or (byte)'?' && IsBlankOrBreakOrEnd(PeekAt(1)))
                return false;

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
    /// Advances past a flow collection that opens at the cursor and closes on the same line, skipping its nested
    /// collections and quoted scalars, for probing.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the collection closes on the line, with the cursor after its closing bracket;
    /// <see langword="false" /> when the line ends first.
    /// </returns>
    private bool SkipFlowCollectionOnLine()
    {
        int depth = 0;
        while (!IsBreakOrEnd(Peek()))
        {
            byte b = Peek();
            if (b is (byte)'"' or (byte)'\'')
            {
                if (!SkipQuotedOnLine(b))
                    return false;

                continue;
            }

            _pos++;
            if (b is (byte)'[' or (byte)'{')
            {
                depth++;
            }
            else if (b is (byte)']' or (byte)'}' && --depth == 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies that a node property just read is followed by white space, a line break, or the end of the input, or in
    /// flow context by the comma or closing bracket that ends the node.
    /// </summary>
    /// <param name="flow">Whether the property is in flow context.</param>
    /// <exception cref="YamlFormatException">The property is joined to the content that follows it.</exception>
    private void RequirePropertySeparation(bool flow)
    {
        byte c = Peek();
        if (IsBlankOrBreakOrEnd(c) || (flow && c is (byte)',' or (byte)']' or (byte)'}'))
            return;

        throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
    }

    /// <summary>
    /// Parses an alias node (<c>*name</c>) and appends an unresolved alias row.
    /// </summary>
    /// <returns>The row index of the alias.</returns>
    private int ParseAlias()
    {
        int offset = _pos;
        string name = ReadName(1);
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
    /// <exception cref="YamlFormatException">The name is empty.</exception>
    private string ReadName(int skip)
    {
        int indicator = _pos;
        for (int i = 0; i < skip; i++)
            Advance();

        int start = _pos;
        while (!IsBlankOrBreakOrEnd(Peek()) && Peek() is not ((byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}'))
            _pos++;

        if (_pos == start)
            throw ErrorAt(indicator, YamlResourceStrings.Format_Invalid_YamlInvalidAnchorName);

        return Utf8(start, _pos - start);
    }

    /// <summary>
    /// Reads a tag node property: a verbatim tag to its closing <c>&gt;</c>, or a shorthand tag up to the next
    /// separator or flow indicator.
    /// </summary>
    /// <returns>The captured tag text.</returns>
    /// <exception cref="YamlFormatException">A verbatim tag has no closing <c>&gt;</c>.</exception>
    private string ReadTag()
    {
        int start = _pos;

        // A verbatim tag may hold any URI character, commas and brackets included, so only its '>' ends it.
        if (PeekAt(1) == (byte)'<')
        {
            _pos += 2;
            while (!IsBlankOrBreakOrEnd(Peek()) && Peek() != (byte)'>')
                _pos++;

            if (Peek() != (byte)'>')
                throw ErrorAt(start, YamlResourceStrings.Format_Invalid_YamlInvalidTag);

            _pos++;
            return Utf8(start, _pos - start);
        }

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
        YamlReaderRow p = _rows[parent];
        if (p.FirstChild < 0)
        {
            p.FirstChild = child;
            p.LastChild = child;
        }
        else
        {
            YamlReaderRow last = _rows[p.LastChild];
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
    /// Creates a single-line plain scalar row from its source text, resolving the text's implicit type under the active
    /// schema. A scalar that resolves to a value other than a string keeps the length of its text, so that a tag or a
    /// mapping key can read it as written.
    /// </summary>
    /// <param name="start">The byte offset of the scalar's first content byte, where the scalar begins.</param>
    /// <param name="end">The byte offset just past the scalar's last content byte.</param>
    /// <param name="anchor">The anchor name, or <see langword="null" />.</param>
    /// <param name="tag">The tag, or <see langword="null" />.</param>
    /// <returns>The row index of the scalar.</returns>
    private int NewPlainScalar(int start, int end, string? anchor, string? tag)
    {
        YamlValueKind kind = YamlScalarResolver.Resolve(_source.AsSpan(start, end - start), _version, out long bits);
        if (kind == YamlValueKind.String)
            return NewString(Utf8(start, end - start), YamlScalarStyle.Plain, start, anchor, tag);

        int row = NewScalar(kind, bits, YamlScalarStyle.Plain, start, anchor, tag);
        YamlReaderRow r = _rows[row];
        r.SourceLength = end - start;
        _rows[row] = r;
        return row;
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
        int index = _strings.Count;
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
