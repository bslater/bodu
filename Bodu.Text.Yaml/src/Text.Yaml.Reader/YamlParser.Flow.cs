// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Flow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Flow-context parsing for the YAML parser: flow sequences (<c>[...]</c>), flow mappings (<c>{...}</c>), and flow
/// scalars, all of which may span lines and contain interleaved comments.
/// </summary>
internal sealed partial class YamlParser
{
    /// <summary>
    /// Parses a flow node entered from block context, establishing the continuation-indentation floor for the duration
    /// of the flow.
    /// </summary>
    /// <param name="parentIndent">The indentation of the block node that owns the flow collection.</param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseFlowNodeFromBlock(int parentIndent)
    {
        int previous = _flowIndent;
        _flowIndent = parentIndent;
        try
        {
            return ParseFlowNode();
        }
        finally
        {
            _flowIndent = previous;
        }
    }

    /// <summary>
    /// Parses a flow node, dispatching to a flow sequence, flow mapping, alias, or flow scalar.
    /// </summary>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseFlowNode()
    {
        SkipFlowWhitespace();
        string? anchor = TryReadAnchorAndTag(out string? tag);
        SkipFlowWhitespace();

        byte c = Peek();
        int row;
        if (c == (byte)'[')
            row = ParseFlowSequence();
        else if (c == (byte)'{')
            row = ParseFlowMapping();
        else if (c == (byte)'*')
            row = ParseAlias();
        else
            row = ParseFlowScalar();

        ApplyProperties(row, anchor, tag);
        return row;
    }

    /// <summary>
    /// Parses a flow sequence, supporting plain elements and implicit single-pair mapping entries, counting it toward
    /// the nesting depth.
    /// </summary>
    /// <returns>The row index of the sequence.</returns>
    /// <exception cref="YamlFormatException">The sequence would nest deeper than the maximum depth.</exception>
    private int ParseFlowSequence()
    {
        EnterCollection();
        try
        {
            return ParseFlowSequenceCore();
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a flow sequence, supporting plain elements and implicit single-pair mapping entries, after the depth
    /// guard has been applied.
    /// </summary>
    /// <returns>The row index of the sequence.</returns>
    private int ParseFlowSequenceCore()
    {
        int sequence = NewContainer(YamlReaderNodeKind.Sequence, _pos, null, null);
        Advance(); // '['
        SkipFlowWhitespace();

        if (Peek() == (byte)']')
        {
            Advance();
            return sequence;
        }

        while (true)
        {
            // A leading or consecutive comma denotes a missing (empty) element, which is not permitted.
            if (Peek() == (byte)',')
                throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidFlow);

            // '?' followed by a separator introduces the explicit key of a single-pair mapping entry, as in a flow
            // mapping; otherwise it begins a plain scalar.
            bool explicitKey = Peek() == (byte)'?' && IsFlowColonStop(PeekAt(1));
            if (explicitKey)
            {
                Advance();
                SkipFlowWhitespace();
            }

            // An implicit key must lie on its ':' indicator's line, so the line on which the element starts is kept.
            int elementLine = _line;
            int element = ParseFlowNode();
            SkipFlowWhitespace();

            // A single-pair mapping element: `key: value` inside the sequence.
            if (AtFlowValueIndicator(element))
            {
                // An implicit key and its ':' indicator must be on the same line; an explicit key's may follow later.
                if (!explicitKey && _line != elementLine)
                    throw Error(YamlResourceStrings.Format_Invalid_YamlMultilineImplicitKey);

                Advance();
                SkipFlowWhitespace();
                element = ParseFlowPair(element, hasValue: true);
                SkipFlowWhitespace();
            }
            else if (explicitKey)
            {
                // An explicit key without a ':' value indicator has a null value.
                element = ParseFlowPair(element, hasValue: false);
            }

            AppendChild(sequence, element);

            byte c = Peek();
            if (c == (byte)',')
            {
                Advance();
                SkipFlowWhitespace();
                if (Peek() == (byte)']')
                {
                    Advance();
                    break;
                }

                continue;
            }

            if (c == (byte)']')
            {
                Advance();
                break;
            }

            throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidFlow);
        }

        return sequence;
    }

    /// <summary>
    /// Builds the single-pair mapping of a flow sequence entry around its already parsed key, parsing the value that
    /// follows its <c>:</c> value indicator, and counts the mapping toward the nesting depth.
    /// </summary>
    /// <param name="key">The row index of the parsed key node.</param>
    /// <param name="hasValue">
    /// Whether a <c>:</c> value indicator was consumed, so that a value may follow; an explicit key without one has a
    /// null value.
    /// </param>
    /// <returns>The row index of the single-pair mapping.</returns>
    /// <exception cref="YamlFormatException">The mapping would nest deeper than the maximum depth.</exception>
    private int ParseFlowPair(int key, bool hasValue)
    {
        EnterCollection();
        try
        {
            int pair = NewContainer(YamlReaderNodeKind.Mapping, _rows[key].Offset, null, null);
            string keyText = KeyTextOf(key);
            int value = !hasValue || Peek() is (byte)',' or (byte)']'
                ? NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null)
                : ParseFlowNode();

            YamlReaderRow v = _rows[value];
            v.Key = keyText;
            _rows[value] = v;
            AppendChild(pair, value);
            return pair;
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a flow mapping of comma-separated key/value entries, counting it toward the nesting depth.
    /// </summary>
    /// <returns>The row index of the mapping.</returns>
    /// <exception cref="YamlFormatException">The mapping would nest deeper than the maximum depth.</exception>
    private int ParseFlowMapping()
    {
        EnterCollection();
        try
        {
            return ParseFlowMappingCore();
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a flow mapping of comma-separated key/value entries, after the depth guard has been applied.
    /// </summary>
    /// <returns>The row index of the mapping.</returns>
    private int ParseFlowMappingCore()
    {
        int mapping = NewContainer(YamlReaderNodeKind.Mapping, _pos, null, null);
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        Advance(); // '{'
        SkipFlowWhitespace();

        if (Peek() == (byte)'}')
        {
            Advance();
            return mapping;
        }

        while (true)
        {
            // A leading or consecutive comma denotes a missing (empty) entry, which is not permitted.
            if (Peek() == (byte)',')
                throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidFlow);

            // '?' introduces an explicit key only when followed by a separator; otherwise it begins a plain scalar.
            if (Peek() == (byte)'?' && IsFlowColonStop(PeekAt(1)))
            {
                Advance();
                SkipFlowWhitespace();
            }

            int keyNode = ParseFlowNode();
            string key = KeyTextOf(keyNode);
            SkipFlowWhitespace();

            int value;
            if (AtFlowValueIndicator(keyNode))
            {
                Advance();
                SkipFlowWhitespace();
                value = Peek() is (byte)',' or (byte)'}'
                    ? NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null)
                    : ParseFlowNode();
            }
            else
            {
                value = NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);
            }

            AddMappingChild(mapping, value, key, keyIndex);

            SkipFlowWhitespace();
            byte c = Peek();
            if (c == (byte)',')
            {
                Advance();
                SkipFlowWhitespace();
                if (Peek() == (byte)'}')
                {
                    Advance();
                    break;
                }

                continue;
            }

            if (c == (byte)'}')
            {
                Advance();
                break;
            }

            throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidFlow);
        }

        return mapping;
    }

    /// <summary>
    /// Parses a flow scalar: a quoted scalar or a restricted single-line plain scalar.
    /// </summary>
    /// <returns>The row index of the scalar.</returns>
    private int ParseFlowScalar()
    {
        int offset = _pos;
        byte c = Peek();
        if (c == (byte)'"')
            return NewString(ReadDoubleQuoted(-1), YamlScalarStyle.DoubleQuoted, offset, null, null);

        if (c == (byte)'\'')
            return NewString(ReadSingleQuoted(-1), YamlScalarStyle.SingleQuoted, offset, null, null);

        // An empty node ends where it begins; any other plain scalar cannot begin with an indicator, nor with '-', '?'
        // or ':' followed by a separator or a flow indicator.
        bool empty = IsBreakOrEnd(c) || c is (byte)',' or (byte)']' or (byte)'}'
            || (c == (byte)':' && IsFlowColonStop(PeekAt(1)));
        if (!empty && !CanBeginPlainScalar(flow: true))
            throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        int start = _pos;
        int end = ScanFlowPlainLine();

        // A plain scalar may continue on following lines, which fold as in block context: a line break into a space,
        // and each blank line into a line feed. A folded scalar is always a string.
        StringBuilder? folded = null;
        while (IsLineBreak(Peek()) && TryContinueFlowPlainScalar(out int blankLines))
        {
            folded ??= new StringBuilder(Utf8(start, end - start));
            folded.Append(blankLines == 0 ? " " : new string('\n', blankLines));

            int lineStart = _pos;
            int lineEnd = ScanFlowPlainLine();
            folded.Append(Utf8(lineStart, lineEnd - lineStart));
        }

        if (folded is not null)
            return NewString(folded.ToString(), YamlScalarStyle.Plain, offset, null, null);

        ReadOnlySpan<byte> span = _source.AsSpan(start, end - start);
        YamlValueKind kind = YamlScalarResolver.Resolve(span, _version, out long bits);
        return kind != YamlValueKind.String
            ? NewScalar(kind, bits, YamlScalarStyle.Plain, offset, null, null)
            : NewString(Utf8(start, end - start), YamlScalarStyle.Plain, offset, null, null);
    }

    /// <summary>
    /// Advances over one line of a plain scalar in flow context, stopping at a line break, a flow indicator, a value
    /// indicator, or a comment.
    /// </summary>
    /// <returns>The byte offset just past the line's last non-space content byte.</returns>
    private int ScanFlowPlainLine()
    {
        int end = _pos;
        while (!IsBreakOrEnd(Peek()))
        {
            byte b = Peek();
            if (b is (byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}')
                break;

            if (b == (byte)':' && IsFlowColonStop(PeekAt(1)))
                break;

            if (b is (byte)' ' or (byte)'\t' && PeekAt(1) == (byte)'#')
                break;

            _pos++;
            if (_source[_pos - 1] is not ((byte)' ' or (byte)'\t'))
                end = _pos;
        }

        return end;
    }

    /// <summary>
    /// Determines whether a plain scalar in flow context continues after the line break at the cursor, moving the
    /// cursor to the continuation line's first content byte when it does and leaving it at the line break when it does
    /// not.
    /// </summary>
    /// <param name="blankLines">
    /// When the method returns, the number of blank lines before the continuation line.
    /// </param>
    /// <returns><see langword="true" /> when the scalar continues on a following line.</returns>
    /// <remarks>
    /// The scalar ends at a line that begins with a flow indicator, a value indicator, or a comment, at the end of the
    /// input, and at a line that the flow collection's own rules reject (indented by a tab, not indented past the
    /// owning block node, or a document marker), which the collection then reports.
    /// </remarks>
    private bool TryContinueFlowPlainScalar(out int blankLines)
    {
        int savePos = _pos;
        int saveLine = _line;
        int saveLineStart = _lineStart;

        blankLines = 0;
        Advance(); // the line break ending the previous line
        while (true)
        {
            int p = _pos;
            while (p < _length && _source[p] is (byte)' ' or (byte)'\t')
                p++;

            if (p < _length && IsLineBreak(_source[p]))
            {
                _pos = p;
                Advance();
                blankLines++;
                continue;
            }

            _pos = p;
            break;
        }

        byte c = Peek();
        bool continues = !AtEnd
            && _source[_lineStart] != (byte)'\t'
            && (_flowIndent < 0 || CurrentColumn() > _flowIndent)
            && !(CurrentColumn() == 0 && AtDocumentBoundary())
            && c is not ((byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'#')
            && !(c == (byte)':' && IsFlowColonStop(PeekAt(1)));

        if (!continues)
        {
            _pos = savePos;
            _line = saveLine;
            _lineStart = saveLineStart;
        }

        return continues;
    }

    /// <summary>
    /// Determines whether a byte is a line feed or carriage return.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte begins a line break.</returns>
    private static bool IsLineBreak(byte b) => b is (byte)'\n' or (byte)'\r';

    /// <summary>
    /// Determines whether the cursor is at the value indicator of a flow mapping entry whose key has been parsed.
    /// </summary>
    /// <param name="key">The row index of the parsed key node.</param>
    /// <returns><see langword="true" /> when a <c>:</c> at the cursor begins the key's value.</returns>
    /// <remarks>
    /// After a JSON-like key (a quoted scalar or a flow collection) the <c>:</c> may be followed directly by the value.
    /// After any other key it must be followed by a separator or a flow indicator, since a <c>:</c> followed by another
    /// character could continue a plain scalar.
    /// </remarks>
    private bool AtFlowValueIndicator(int key)
    {
        if (Peek() != (byte)':')
            return false;

        if (IsFlowColonStop(PeekAt(1)))
            return true;

        YamlReaderRow row = _rows[key];
        return row.Kind is YamlReaderNodeKind.Sequence or YamlReaderNodeKind.Mapping
            || (row.Kind == YamlReaderNodeKind.Scalar
                && row.ScalarStyle is YamlScalarStyle.DoubleQuoted or YamlScalarStyle.SingleQuoted);
    }

    /// <summary>
    /// Determines whether a byte following a colon terminates a plain scalar in flow context.
    /// </summary>
    /// <param name="b">The byte after the colon.</param>
    /// <returns><see langword="true" /> when the colon acts as a value indicator.</returns>
    private static bool IsFlowColonStop(byte b) =>
        IsBlankOrBreakOrEnd(b) || b is (byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}';

    /// <summary>
    /// Advances over spaces, tabs, line breaks, and comments between flow tokens.
    /// </summary>
    private void SkipFlowWhitespace()
    {
        bool crossed = false;
        while (!AtEnd)
        {
            byte b = Peek();
            if (b is (byte)' ' or (byte)'\t')
            {
                _pos++;
                continue;
            }

            if (b is (byte)'\n' or (byte)'\r')
            {
                Advance();
                crossed = true;

                // A tab cannot indent a non-empty flow continuation line (a blank tab-only line is immaterial).
                if (Peek() == (byte)'\t')
                {
                    int q = _pos;
                    while (q < _length && _source[q] is (byte)' ' or (byte)'\t')
                        q++;

                    if (q < _length && _source[q] is not ((byte)'\n' or (byte)'\r'))
                        throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlTabIndentation);
                }

                continue;
            }

            if (b == (byte)'#')
            {
                RequireCommentSpacing();
                while (!IsBreakOrEnd(Peek()))
                    _pos++;

                continue;
            }

            break;
        }

        // A document-start or document-end marker cannot appear inside a flow collection.
        if (crossed && CurrentColumn() == 0 && AtDocumentBoundary())
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        // Flow content continued onto a new line must be indented more than the owning block node; a closing
        // delimiter may sit at the owning node's indentation, but not to the left of it.
        if (crossed && _flowIndent >= 0 && !AtEnd)
        {
            int column = CurrentColumn();
            if (Peek() is (byte)']' or (byte)'}' ? column < _flowIndent : column <= _flowIndent)
                throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlInvalidIndentation);
        }
    }
}
