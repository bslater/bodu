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
    private readonly YamlDuplicateKeyBehavior _duplicateKeyBehavior;
    private readonly YamlMergeKeyBehavior _mergeKeyBehavior;
    private List<YamlReaderRow> _rows = [];

    private int _pos;
    private int _line;
    private int _lineStart;
    private int _depth;
    private Dictionary<string, string>? _tagHandles;
    private int _flowIndent = -1;
    private int _lastPropertyLine = -1;
    private bool _documentStartConsumed;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlParser" /> class over the specified source.
    /// </summary>
    /// <param name="source">The UTF-8 source buffer. The parser reads but does not modify it.</param>
    /// <param name="length">The number of valid bytes in <paramref name="source" />.</param>
    /// <param name="version">The specification version whose resolution rules apply.</param>
    /// <param name="maxDepth">The maximum container nesting depth permitted.</param>
    /// <param name="duplicateKeyBehavior">The policy applied to duplicate mapping keys.</param>
    /// <param name="mergeKeyBehavior">The policy applied to the merge key.</param>
    internal YamlParser(
        byte[] source,
        int length,
        YamlSpecVersion version,
        int maxDepth,
        YamlDuplicateKeyBehavior duplicateKeyBehavior = YamlDuplicateKeyBehavior.Throw,
        YamlMergeKeyBehavior mergeKeyBehavior = YamlMergeKeyBehavior.Expand)
    {
        _source = source;
        _length = length;
        _version = version;
        _maxDepth = maxDepth;
        _duplicateKeyBehavior = duplicateKeyBehavior;
        _mergeKeyBehavior = mergeKeyBehavior;
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
        ValidateSource();
        SkipByteOrderMark();
        SkipDirectivesAndDocumentStart();

        SkipBlankCommentLines();

        ParseDocumentBody();

        // Trailing content after the first document (other than a document boundary) is not permitted here.
        SkipBlankCommentLines();
        if (!AtStreamEnd() && !AtDocumentBoundary())
            throw Error(YamlResourceStrings.Format_Invalid_YamlContentAfterDocumentEnd);

        // A document-end marker line may carry only a trailing comment.
        if (!AtStreamEnd() && Peek() == (byte)'.')
        {
            Advance();
            Advance();
            Advance();
            SkipSpaces();
            if (!IsBreakOrEnd(Peek()) && Peek() != (byte)'#')
                throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
        }

        Compose();
        return _rows;
    }

    /// <summary>
    /// Parses every document in a multi-document stream, returning each document's independent row store and
    /// decoded-string table.
    /// </summary>
    /// <returns>The parsed documents, in stream order. An empty stream yields a single null document.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    internal List<(List<YamlReaderRow> Rows, string[] Strings)> ParseStream()
    {
        var documents = new List<(List<YamlReaderRow>, string[])>();
        ValidateSource();
        SkipByteOrderMark();

        while (true)
        {
            SkipDirectivesAndDocumentStart();
            SkipBlankCommentLines();

            // An empty region with no explicit '---' start and no content is not a document.
            if (!_documentStartConsumed && (AtStreamEnd() || (CurrentColumn() == 0 && Peek() == (byte)'.' && AtDocumentBoundary())))
            {
                if (!AtEnd && Peek() == (byte)'.')
                {
                    while (!IsBreakOrEnd(Peek()))
                        _pos++;

                    if (!AtEnd)
                        Advance();
                }

                SkipBlankCommentLines();
                if (AtStreamEnd())
                    break;

                continue;
            }

            _rows = [];
            _strings = [];

            ParseDocumentBody();
            Compose();
            documents.Add((_rows, _strings.ToArray()));

            SkipBlankCommentLines();

            // Consume a document-end marker ('...') line so the next iteration starts cleanly.
            if (!AtEnd && CurrentColumn() == 0 && Peek() == (byte)'.' && AtDocumentBoundary())
            {
                while (!IsBreakOrEnd(Peek()))
                    _pos++;

                if (!AtEnd)
                    Advance();
            }

            SkipBlankCommentLines();
            if (AtStreamEnd())
                break;
        }

        return documents;
    }

    /// <summary>
    /// Parses the body of a single document into the current row store, defaulting an empty body to null.
    /// </summary>
    private void ParseDocumentBody()
    {
        _depth = 0;

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
    }

    /// <summary>
    /// Parses a block-context node: a block mapping, a block sequence, or a scalar, after consuming any node properties
    /// (anchor and tag).
    /// </summary>
    /// <param name="minIndent">The minimum column at which the node's content must appear.</param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseBlockNode(int minIndent)
    {
        if (++_depth > _maxDepth)
            throw Error(string.Format(System.Globalization.CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlNestingTooDeep, _maxDepth));

        try
        {
            return ParseBlockNodeCore(minIndent);
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a block-context node after the depth guard has been applied.
    /// </summary>
    /// <param name="minIndent">The minimum column at which the node's content must appear.</param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseBlockNodeCore(int minIndent)
    {
        SkipBlankCommentLines();

        // In block context the anchor and tag node properties may be spread across line breaks before the node.
        var anchor = TryReadAnchorAndTag(out var tag, crossLines: true);

        SkipBlankCommentLines();

        if (AtStreamEnd() || AtDocumentBoundary())
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        var col = CurrentColumn();
        if (col < minIndent)
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        var c = Peek();

        // A block sequence entry: '-' followed by a space, a line break, or end of input.
        if (c == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
        {
            // A block sequence cannot begin on the same line as the node's anchor or tag.
            if ((anchor is not null || tag is not null) && _line == _lastPropertyLine)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

            return Finish(ParseBlockSequence(col, anchor, tag), anchor);
        }

        // A flow collection introducer.
        if (c == (byte)'[' || c == (byte)'{')
        {
            var flow = ParseFlowNodeFromBlock(-1);
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        // An alias node.
        if (c == (byte)'*')
        {
            if (anchor is not null || tag is not null)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

            var alias = ParseAlias();
            SkipLineTrailing();
            return Finish(alias, anchor);
        }

        // A block scalar (literal or folded). The floor is the parent node's indentation (one less than the column at
        // which this node's content must appear), so a less-indented following line ends the scalar.
        if (c == (byte)'|' || c == (byte)'>')
            return Finish(ParseBlockScalar(minIndent - 1, anchor, tag), anchor);

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

            // A node already carrying a different anchor would mean two anchors were written for one node.
            if (r.Anchor is not null && !string.Equals(r.Anchor, anchor, StringComparison.Ordinal))
                throw Error(YamlResourceStrings.Format_Invalid_YamlMultipleAnchors);

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
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);

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

            AddMappingChild(mapping, valueRow, keyText, keyIndex);

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

        // A block sequence cannot begin on the same line as the mapping value indicator.
        if (c == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
            throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        if (c == (byte)'[' || c == (byte)'{')
        {
            var flow = ParseFlowNodeFromBlock(keyIndent);
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        if (c == (byte)'*')
        {
            if (anchor is not null || tag is not null)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

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

            var separationHasTab = false;
            while (Peek() is (byte)' ' or (byte)'\t')
            {
                if (Peek() == (byte)'\t')
                    separationHasTab = true;

                _pos++;
            }

            // A tab cannot indent a nested block sequence entry that begins on the dash's line.
            if (separationHasTab && Peek() == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
                throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlTabIndentation);

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

        // A sequence, mapping, or alias used as a key cannot be represented in the JSON-compatible tree profile.
        throw Error(YamlResourceStrings.Format_Invalid_YamlComplexKeyUnsupported);
    }

    /// <summary>
    /// Adds a key/value entry to a mapping, applying the configured duplicate-key policy.
    /// </summary>
    /// <param name="mapping">The mapping row index.</param>
    /// <param name="valueRow">The row index of the entry's value node.</param>
    /// <param name="keyText">The decoded key text.</param>
    /// <param name="index">The key-to-child index tracking the mapping's existing keys.</param>
    /// <exception cref="YamlFormatException">
    /// The key duplicates an existing key and the policy is <see cref="YamlDuplicateKeyBehavior.Throw" />.
    /// </exception>
    private void AddMappingChild(int mapping, int valueRow, string keyText, Dictionary<string, int> index)
    {
        var v = _rows[valueRow];
        v.Key = keyText;
        _rows[valueRow] = v;

        if (index.TryGetValue(keyText, out var existing))
        {
            switch (_duplicateKeyBehavior)
            {
                case YamlDuplicateKeyBehavior.UseFirst:
                    return;

                case YamlDuplicateKeyBehavior.UseLast:
                    RemoveChild(mapping, existing);
                    break;

                default:
                    throw Error(YamlResourceStrings.Format_Invalid_YamlDuplicateKey);
            }
        }

        AppendChild(mapping, valueRow);
        index[keyText] = valueRow;
    }

    /// <summary>
    /// Removes a child from a container by locating its predecessor and unlinking it.
    /// </summary>
    /// <param name="parent">The container row index.</param>
    /// <param name="child">The child row index to remove.</param>
    private void RemoveChild(int parent, int child)
    {
        var first = _rows[parent].FirstChild;
        if (first == child)
        {
            UnlinkChild(parent, child, -1);
            return;
        }

        var previous = first;
        while (previous >= 0 && _rows[previous].NextSibling != child)
            previous = _rows[previous].NextSibling;

        if (previous >= 0)
            UnlinkChild(parent, child, previous);
    }

    /// <summary>
    /// Creates the format exception for a parse error at the current position.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    private YamlFormatException Error(string message) =>
        new(message, _line + 1, CurrentColumn() + 1, _pos);
}
