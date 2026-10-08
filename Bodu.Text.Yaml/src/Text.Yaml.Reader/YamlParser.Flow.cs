// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Flow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

            int element = ParseFlowNode();
            int elementLine = _line;
            SkipFlowWhitespace();

            // A single-pair mapping element: `key: value` inside the sequence.
            if (Peek() == (byte)':' && IsFlowColonStop(PeekAt(1)))
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
            if (Peek() == (byte)':')
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

        // A plain scalar in flow context cannot consist of a bare '-' followed by a separator or flow indicator.
        if (Peek() == (byte)'-' && (IsBlankOrBreakOrEnd(PeekAt(1)) || PeekAt(1) is (byte)',' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}'))
            throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        int start = _pos;
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

        ReadOnlySpan<byte> span = _source.AsSpan(start, end - start);
        YamlValueKind kind = YamlScalarResolver.Resolve(span, _version, out long bits);
        return kind != YamlValueKind.String
            ? NewScalar(kind, bits, YamlScalarStyle.Plain, offset, null, null)
            : NewString(Utf8(start, end - start), YamlScalarStyle.Plain, offset, null, null);
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
        // delimiter may sit at the parent indentation.
        if (crossed && _flowIndent >= 0 && !AtEnd && CurrentColumn() <= _flowIndent
            && Peek() is not ((byte)']' or (byte)'}'))
        {
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlInvalidIndentation);
        }
    }
}
