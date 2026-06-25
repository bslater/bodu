// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Parses a single YAML document from UTF-8 source into the flat, index-linked row store consumed by the document
/// object models. The parser is a recursive-descent reader over the byte stream that tracks indentation to recover
/// block structure, and handles flow collections, all scalar presentation styles, comments, anchors, tags, and aliases.
/// </summary>
/// <remarks>
/// The parser materializes one document. Stream-level concerns (multiple documents separated by <c>---</c>, alias
/// resolution across the whole document, and merge-key expansion) are layered on top of the rows it produces.
/// </remarks>
internal sealed partial class YamlParser
{
    private readonly byte[] _source;
    private readonly int _length;
    private readonly YamlSpecVersion _version;
    private readonly int _maxDepth;
    private readonly List<YamlReaderRow> _rows = [];

    private int _pos;
    private int _line;
    private int _lineStart;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlParser" /> class over the specified source.
    /// </summary>
    /// <param name="source">The UTF-8 source buffer. The parser reads but does not modify it.</param>
    /// <param name="length">The number of valid bytes in <paramref name="source" />.</param>
    /// <param name="version">The specification version whose resolution rules apply.</param>
    /// <param name="maxDepth">The maximum container nesting depth permitted.</param>
    internal YamlParser(byte[] source, int length, YamlSpecVersion version, int maxDepth)
    {
        _source = source;
        _length = length;
        _version = version;
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Gets the row store after a successful parse.
    /// </summary>
    /// <value>The list of parsed rows, with the document root at index zero.</value>
    internal List<YamlReaderRow> Rows => _rows;

    /// <summary>
    /// Parses the first document in the source and returns the row store with the root at index zero.
    /// </summary>
    /// <returns>The parsed row store.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    internal List<YamlReaderRow> Parse()
    {
        SkipByteOrderMark();
        SkipDirectivesAndDocumentStart();

        SkipBlankCommentLines();

        if (AtStreamEnd() || AtDocumentBoundary())
        {
            // An empty document resolves to a single null root node at index zero.
            NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);
        }
        else
        {
            // The first node created becomes the root and therefore occupies index zero.
            ParseBlockNode(0);
        }

        // Trailing content after the first document (other than a document boundary) is not permitted here.
        SkipBlankCommentLines();
        if (!AtStreamEnd() && !AtDocumentBoundary())
            throw Error(YamlResourceStrings.Format_Invalid_YamlContentAfterDocumentEnd);

        return _rows;
    }

    /// <summary>
    /// Parses a block-context node: a block mapping, a block sequence, or a scalar, after consuming any node properties
    /// (anchor and tag).
    /// </summary>
    /// <param name="minIndent">The minimum column at which the node's content must appear.</param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseBlockNode(int minIndent)
    {
        SkipBlankCommentLines();

        var anchor = TryReadAnchorAndTag(out var tag);

        SkipBlankCommentLines();

        if (AtStreamEnd() || AtDocumentBoundary())
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        var col = CurrentColumn();
        if (col < minIndent)
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        var c = Peek();

        // A block sequence entry: '-' followed by a space, a line break, or end of input.
        if (c == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
            return Finish(ParseBlockSequence(col, anchor, tag), anchor);

        // A flow collection introducer.
        if (c == (byte)'[' || c == (byte)'{')
        {
            var flow = ParseFlowNode();
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        // An alias node.
        if (c == (byte)'*')
        {
            var alias = ParseAlias();
            SkipLineTrailing();
            return Finish(alias, anchor);
        }

        // A block scalar (literal or folded).
        if (c == (byte)'|' || c == (byte)'>')
            return Finish(ParseBlockScalar(col, anchor, tag), anchor);

        // Otherwise the node is either a block mapping or a single scalar. Decide by probing for a key indicator.
        if (TryDetectBlockMapping(col))
            return Finish(ParseBlockMapping(col, anchor, tag), anchor);

        var scalar = ParseBlockScalarPlainOrQuoted(minIndent);
        ApplyProperties(scalar, anchor, tag);
        return Finish(scalar, anchor);
    }

    /// <summary>
    /// Applies a captured anchor flag to a freshly parsed row and returns it.
    /// </summary>
    /// <param name="row">The row index to finalize.</param>
    /// <param name="anchor">The anchor name captured for the node, or <see langword="null" />.</param>
    /// <returns>The same row index, for fluent use.</returns>
    private int Finish(int row, string? anchor)
    {
        if (anchor is not null)
        {
            var r = _rows[row];
            r.Anchor = anchor;
            r.Flags |= YamlReaderRowFlags.Anchored;
            _rows[row] = r;
        }

        return row;
    }

    /// <summary>
    /// Stamps an anchor and tag onto an already-created row (used for flow and scalar nodes).
    /// </summary>
    /// <param name="row">The row index to annotate.</param>
    /// <param name="anchor">The anchor name, or <see langword="null" />.</param>
    /// <param name="tag">The tag, or <see langword="null" />.</param>
    private void ApplyProperties(int row, string? anchor, string? tag)
    {
        var r = _rows[row];
        if (anchor is not null)
        {
            r.Anchor = anchor;
            r.Flags |= YamlReaderRowFlags.Anchored;
        }

        if (tag is not null)
            r.Tag = tag;

        _rows[row] = r;
    }

    /// <summary>
    /// Parses a block mapping whose entries begin at the given indentation.
    /// </summary>
    /// <param name="indent">The column at which each key begins.</param>
    /// <param name="anchor">The anchor captured for the mapping node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the mapping node, or <see langword="null" />.</param>
    /// <returns>The row index of the mapping.</returns>
    private int ParseBlockMapping(int indent, string? anchor, string? tag)
    {
        var mapping = NewContainer(YamlReaderNodeKind.Mapping, _pos, anchor, tag);

        while (true)
        {
            SkipBlankCommentLines();
            if (AtStreamEnd() || AtDocumentBoundary())
                break;

            if (CurrentColumn() != indent)
                break;

            // Explicit key indicator '? key' / ': value'. Handle the common explicit-key form.
            var keyRow = ParseMappingKey(indent, out var keyText);

            ExpectValueIndicator();

            var valueRow = ParseMappingValue(indent);

            var pair = _rows[valueRow];
            pair.Key = keyText;
            _rows[valueRow] = pair;
            AppendChild(mapping, valueRow);

            _ = keyRow;
        }

        return mapping;
    }

    /// <summary>
    /// Parses the key portion of a block mapping entry, supporting plain and quoted simple keys and the explicit
    /// <c>?</c> key indicator.
    /// </summary>
    /// <param name="indent">The mapping indentation, used for explicit keys that span the value indicator.</param>
    /// <param name="keyText">When the method returns, the decoded key text.</param>
    /// <returns>
    /// The row index of the parsed key node (retained for completeness; simple keys decode to text only).
    /// </returns>
    private int ParseMappingKey(int indent, out string keyText)
    {
        if (Peek() == (byte)'?' && IsBlankOrBreakOrEnd(PeekAt(1)))
        {
            Advance();
            SkipSpaces();
            var keyNode = ParseBlockNode(indent + 1);
            keyText = KeyTextOf(keyNode);
            SkipBlankCommentLines();
            return keyNode;
        }

        keyText = ReadSimpleKey();
        return -1;
    }

    /// <summary>
    /// Parses the value portion of a block mapping entry, which may appear on the same line or on more-indented
    /// following lines.
    /// </summary>
    /// <param name="keyIndent">The indentation of the entry's key.</param>
    /// <returns>The row index of the value node.</returns>
    private int ParseMappingValue(int keyIndent)
    {
        SkipSpaces();

        // Value on the same line.
        if (!IsBreakOrEnd(Peek()) && Peek() != (byte)'#')
            return ParseInlineOrBlockValue(keyIndent);

        SkipLineTrailing();
        SkipBlankCommentLines();

        if (AtStreamEnd() || AtDocumentBoundary())
            return NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);

        var col = CurrentColumn();
        if (col <= keyIndent)
            return NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);

        return ParseBlockNode(keyIndent + 1);
    }

    /// <summary>
    /// Parses a value that begins on the current line, dispatching to flow, alias, block-scalar, or scalar parsing, and
    /// recognizing a nested block sequence that begins at the key's own column on following lines.
    /// </summary>
    /// <param name="keyIndent">The indentation of the entry's key.</param>
    /// <returns>The row index of the value node.</returns>
    private int ParseInlineOrBlockValue(int keyIndent)
    {
        var anchor = TryReadAnchorAndTag(out var tag);
        SkipSpaces();

        var c = Peek();
        if (IsBreakOrEnd(c) || c == (byte)'#')
        {
            SkipLineTrailing();
            SkipBlankCommentLines();

            // A block sequence may be indented at the same column as its key.
            if (!AtStreamEnd() && !AtDocumentBoundary() && CurrentColumn() >= keyIndent
                && Peek() == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
            {
                return Finish(ParseBlockSequence(CurrentColumn(), anchor, tag), anchor);
            }

            if (AtStreamEnd() || AtDocumentBoundary() || CurrentColumn() <= keyIndent)
                return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

            return Finish(ParseBlockNode(keyIndent + 1), anchor);
        }

        if (c == (byte)'[' || c == (byte)'{')
        {
            var flow = ParseFlowNode();
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        if (c == (byte)'*')
        {
            var alias = ParseAlias();
            SkipLineTrailing();
            return Finish(alias, anchor);
        }

        if (c == (byte)'|' || c == (byte)'>')
            return Finish(ParseBlockScalar(keyIndent, anchor, tag), anchor);

        var scalar = ParseBlockScalarPlainOrQuoted(keyIndent + 1);
        ApplyProperties(scalar, anchor, tag);
        return Finish(scalar, anchor);
    }

    /// <summary>
    /// Parses a block sequence whose entries begin at the given indentation.
    /// </summary>
    /// <param name="indent">The column at which each <c>-</c> entry marker appears.</param>
    /// <param name="anchor">The anchor captured for the sequence node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the sequence node, or <see langword="null" />.</param>
    /// <returns>The row index of the sequence.</returns>
    private int ParseBlockSequence(int indent, string? anchor, string? tag)
    {
        var sequence = NewContainer(YamlReaderNodeKind.Sequence, _pos, anchor, tag);

        while (true)
        {
            SkipBlankCommentLines();
            if (AtStreamEnd() || AtDocumentBoundary())
                break;

            if (CurrentColumn() != indent || Peek() != (byte)'-' || !IsBlankOrBreakOrEnd(PeekAt(1)))
                break;

            Advance(); // consume '-'
            var entryColumn = CurrentColumn();
            SkipSpaces();

            int element;
            if (IsBreakOrEnd(Peek()) || Peek() == (byte)'#')
            {
                SkipLineTrailing();
                SkipBlankCommentLines();
                if (!AtStreamEnd() && !AtDocumentBoundary() && CurrentColumn() > entryColumn)
                    element = ParseBlockNode(entryColumn + 1);
                else
                    element = NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);
            }
            else
            {
                // Content on the same line as the dash; its effective indent is the column after the dash.
                element = ParseBlockNode(entryColumn + 1);
            }

            AppendChild(sequence, element);
        }

        return sequence;
    }

    /// <summary>
    /// Reads the textual form of a node that is used as a mapping key, decoding scalar keys to text.
    /// </summary>
    /// <param name="row">The row index of the key node.</param>
    /// <returns>The decoded key text, or a synthesized representation for non-scalar keys.</returns>
    private string KeyTextOf(int row)
    {
        var r = _rows[row];
        if (r.Kind == YamlReaderNodeKind.Scalar && r.ValueKind == YamlValueKind.String)
            return MaterializeString(r);

        if (r.Kind == YamlReaderNodeKind.Scalar)
            return r.ValueKind switch
            {
                YamlValueKind.Null => string.Empty,
                YamlValueKind.Boolean => r.AsBoolean() ? "true" : "false",
                YamlValueKind.Integer => r.AsInt64().ToString(CultureInfo.InvariantCulture),
                YamlValueKind.Float => r.AsDouble().ToString(CultureInfo.InvariantCulture),
                _ => string.Empty,
            };

        return string.Empty;
    }

    /// <summary>
    /// Creates the format exception for a parse error at the current position.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    private YamlFormatException Error(string message) =>
        new(message, _line + 1, CurrentColumn() + 1, _pos);
}
