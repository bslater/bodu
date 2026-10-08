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
    /// <summary>The UTF-8 source buffer being parsed.</summary>
    private readonly byte[] _source;

    /// <summary>The number of valid bytes in <see cref="_source" />.</summary>
    private readonly int _length;

    /// <summary>The specification version requested through the options.</summary>
    private readonly YamlSpecVersion _optionVersion;

    /// <summary>The maximum container nesting depth permitted.</summary>
    private readonly int _maxDepth;

    /// <summary>The policy applied to duplicate mapping keys.</summary>
    private readonly YamlDuplicateKeyBehavior _duplicateKeyBehavior;

    /// <summary>The policy applied to the merge key (<c>&lt;&lt;</c>).</summary>
    private readonly YamlMergeKeyBehavior _mergeKeyBehavior;

    /// <summary>The specification version in effect, after any <c>%YAML</c> directive.</summary>
    private YamlSpecVersion _version;

    /// <summary>The row store being populated, with the document root at index zero.</summary>
    private List<YamlReaderRow> _rows = [];

    /// <summary>The byte offset of the cursor within <see cref="_source" />.</summary>
    private int _pos;

    /// <summary>The 0-based line number of the cursor.</summary>
    private int _line;

    /// <summary>The byte offset at which the current line starts, used to compute indentation.</summary>
    private int _lineStart;

    /// <summary>The current container nesting depth.</summary>
    private int _depth;

    /// <summary>The tag-handle map declared by <c>%TAG</c> directives, or <see langword="null" /> when none.</summary>
    private Dictionary<string, string>? _tagHandles;

    /// <summary>The minimum indentation for continuation lines inside the current flow collection, or <c>-1</c> when not in flow context.</summary>
    private int _flowIndent = -1;

    /// <summary>The line on which the most recent anchor or tag property was read, or <c>-1</c> when none has been.</summary>
    private int _lastPropertyLine = -1;

    /// <summary>Indicates whether a leading <c>---</c> document-start marker has already been consumed.</summary>
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
        _optionVersion = version;
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
        SkipDocumentPrefix();
        SkipDirectivesAndDocumentStart();

        SkipDocumentPrefix();

        ParseDocumentBody();
        ParseDocumentEnd();

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

        while (true)
        {
            // Each document may begin with a byte-order mark, before its directives, its '---', or its content.
            SkipDocumentPrefix();
            SkipDirectivesAndDocumentStart();
            SkipDocumentPrefix();

            // An empty region with no explicit '---' start and no content is not a document.
            if (!_documentStartConsumed && (AtStreamEnd() || (CurrentColumn() == 0 && Peek() == (byte)'.' && AtDocumentBoundary())))
            {
                if (!AtEnd && Peek() == (byte)'.')
                    SkipDocumentEndMarker();

                SkipBlankCommentLines();
                if (AtStreamEnd())
                    break;

                continue;
            }

            _rows = [];
            _strings = [];

            // Each document obeys the rules Parse applies to the first: it ends at a document boundary or the end
            // of the stream, so content after its root node cannot begin another document without a marker.
            ParseDocumentBody();
            ParseDocumentEnd();
            Compose();
            documents.Add((_rows, _strings.ToArray()));

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
    /// Verifies that the document just parsed ends at a document boundary or the end of the stream, and consumes the
    /// <c>...</c> document-end marker line that ends it, if any.
    /// </summary>
    /// <remarks>
    /// A document ends only at a <c>---</c> or <c>...</c> marker or at the end of the stream. Content after the root
    /// node, including a directive, therefore cannot follow without a marker: a directive must come after a <c>...</c>
    /// or at the start of the stream.
    /// </remarks>
    /// <exception cref="YamlFormatException">
    /// Content follows the root node without a document marker, or text other than a comment follows the <c>...</c>
    /// marker on its line.
    /// </exception>
    private void ParseDocumentEnd()
    {
        SkipBlankCommentLines();
        if (!AtStreamEnd() && !AtDocumentBoundary())
            throw Error(YamlResourceStrings.Format_Invalid_YamlContentAfterDocumentEnd);

        if (!AtStreamEnd() && Peek() == (byte)'.')
            SkipDocumentEndMarker();
    }

    /// <summary>
    /// Consumes a <c>...</c> document-end marker line at the cursor, which may carry only a trailing comment.
    /// </summary>
    /// <exception cref="YamlFormatException">Text other than a comment follows the marker on its line.</exception>
    private void SkipDocumentEndMarker()
    {
        Advance();
        Advance();
        Advance();
        SkipLineTrailing();
    }

    /// <summary>
    /// Parses a block-context node: a block mapping, a block sequence, or a scalar, after consuming any node properties
    /// (anchor and tag).
    /// </summary>
    /// <param name="minIndent">The minimum column at which the node's content must appear.</param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseBlockNode(int minIndent) =>
        ParseBlockNode(minIndent, minIndent - 1);

    /// <summary>
    /// Parses a block-context node: a block mapping, a block sequence, or a scalar, after consuming any node properties
    /// (anchor and tag), with an explicit parent indentation for a block scalar.
    /// </summary>
    /// <param name="minIndent">The minimum column at which the node's content must appear.</param>
    /// <param name="blockScalarParentIndent">
    /// The indentation of the node's parent, from which a block scalar's content indentation is counted.
    /// </param>
    /// <returns>The row index of the parsed node.</returns>
    private int ParseBlockNode(int minIndent, int blockScalarParentIndent)
    {
        SkipBlankCommentLines();

        // In block context the anchor and tag node properties may be spread across line breaks before the node, on lines
        // indented at least as far as the node's content.
        string? anchor = TryReadAnchorAndTag(out string? tag, crossLines: true, minIndent);

        SkipBlankCommentLines();

        if (AtStreamEnd() || AtDocumentBoundary())
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        int col = CurrentColumn();
        if (col < minIndent)
            return Finish(NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, anchor, tag), anchor);

        byte c = Peek();

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
            int flow = ParseFlowNodeFromBlock(-1);
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        // An alias node.
        if (c == (byte)'*')
        {
            if (anchor is not null || tag is not null)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

            int alias = ParseAlias();
            SkipLineTrailing();
            return Finish(alias, anchor);
        }

        // A block scalar (literal or folded). The floor is the parent node's indentation, so a less-indented following
        // line ends the scalar.
        if (c == (byte)'|' || c == (byte)'>')
            return Finish(ParseBlockScalar(blockScalarParentIndent, anchor, tag), anchor);

        // Otherwise the node is either a block mapping or a single scalar. Decide by probing for a key indicator.
        if (TryDetectBlockMapping(col))
        {
            // An explicit key cannot begin on the same line as the mapping's own anchor or tag.
            if (c == (byte)'?' && IsBlankOrBreakOrEnd(PeekAt(1)) && (anchor is not null || tag is not null)
                && _line == _lastPropertyLine)
            {
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);
            }

            return Finish(ParseBlockMapping(col, anchor, tag), anchor);
        }

        int scalar = ParseBlockScalarPlainOrQuoted(minIndent);
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
            YamlReaderRow r = _rows[row];

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
        YamlReaderRow r = _rows[row];
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
    /// Parses a block mapping whose entries begin at the given indentation, counting it toward the nesting depth.
    /// </summary>
    /// <param name="indent">The column at which each key begins.</param>
    /// <param name="anchor">The anchor captured for the mapping node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the mapping node, or <see langword="null" />.</param>
    /// <returns>The row index of the mapping.</returns>
    /// <exception cref="YamlFormatException">The mapping would nest deeper than the maximum depth.</exception>
    private int ParseBlockMapping(int indent, string? anchor, string? tag)
    {
        EnterCollection();
        try
        {
            return ParseBlockMappingCore(indent, anchor, tag);
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a block mapping whose entries begin at the given indentation, after the depth guard has been applied.
    /// </summary>
    /// <param name="indent">The column at which each key begins.</param>
    /// <param name="anchor">The anchor captured for the mapping node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the mapping node, or <see langword="null" />.</param>
    /// <returns>The row index of the mapping.</returns>
    private int ParseBlockMappingCore(int indent, string? anchor, string? tag)
    {
        int mapping = NewContainer(YamlReaderNodeKind.Mapping, _pos, anchor, tag);
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        while (true)
        {
            SkipBlankCommentLines();
            if (AtStreamEnd() || AtDocumentBoundary())
                break;

            if (CurrentColumn() != indent)
                break;

            // An implicit key is always followed by its ':' value indicator; the ': value' line of an explicit '? key'
            // is optional.
            int keyRow = ParseMappingKey(indent, out string? keyText);

            int valueRow;
            if (keyRow >= 0)
            {
                valueRow = ParseExplicitMappingValue(indent);
            }
            else
            {
                ExpectValueIndicator();
                valueRow = ParseMappingValue(indent);
            }

            AddMappingChild(mapping, valueRow, keyText, keyIndex);
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
    /// The row index of an explicit key's node, or <c>-1</c> for a simple key, which decodes to text only.
    /// </returns>
    private int ParseMappingKey(int indent, out string keyText)
    {
        if (Peek() == (byte)'?' && IsBlankOrBreakOrEnd(PeekAt(1)))
        {
            Advance();
            SkipSpaces();
            int keyNode = ParseBlockNode(indent + 1);
            keyText = KeyTextOf(keyNode);
            SkipBlankCommentLines();
            return keyNode;
        }

        keyText = ReadSimpleKey();
        return -1;
    }

    /// <summary>
    /// Parses the optional value of an explicit-key mapping entry: a <c>:</c> value indicator at the mapping's
    /// indentation, then the value, which may begin on the indicator's line as a compact sequence or mapping.
    /// </summary>
    /// <param name="indent">The mapping indentation, at which the value indicator must appear.</param>
    /// <returns>The row index of the value node, or of a null scalar when the entry has no value line.</returns>
    /// <exception cref="YamlFormatException">
    /// A tab separates the value indicator from a compact sequence or mapping.
    /// </exception>
    private int ParseExplicitMappingValue(int indent)
    {
        // Without a value indicator at the mapping's indentation the key's value is null. A ':' at another column is
        // not this entry's indicator, so the content it begins is left to end the mapping.
        if (AtStreamEnd() || AtDocumentBoundary() || CurrentColumn() != indent
            || Peek() != (byte)':' || !IsBlankOrBreakOrEnd(PeekAt(1)))
        {
            return NewScalar(YamlValueKind.Null, 0, YamlScalarStyle.Plain, _pos, null, null);
        }

        Advance(); // ':'
        int separation = _pos;
        SkipSpaces();

        if (IsBreakOrEnd(Peek()) || Peek() == (byte)'#')
            return ParseMappingValue(indent);

        // A compact sequence or mapping on the indicator's line is indented by the spaces before it, which a tab cannot
        // provide.
        if (_source.AsSpan(separation, _pos - separation).Contains((byte)'\t') && AtCompactCollectionStart())
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlTabIndentation);

        // Otherwise the value is a block node in its own right, so it may be a compact collection at its own column.
        return ParseBlockNode(indent + 1);
    }

    /// <summary>
    /// Determines whether the content at the cursor begins a compact block collection on the current line: a block
    /// sequence entry, or a mapping entry whose simple key is followed by a value indicator.
    /// </summary>
    /// <returns><see langword="true" /> when a compact sequence or mapping begins at the cursor.</returns>
    private bool AtCompactCollectionStart()
    {
        byte c = Peek();
        if (c == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
            return true;

        // Node properties begin a compact mapping when an implicit key follows them on the line.
        if (c is (byte)'&' or (byte)'!')
            return PropertiesPrecedeImplicitKey();

        // Flow collections, aliases, and block scalars begin other nodes, even when a ':' follows later on the line.
        return c is not ((byte)'[' or (byte)'{' or (byte)'*' or (byte)'|' or (byte)'>')
            && TryDetectBlockMapping(CurrentColumn());
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

        int col = CurrentColumn();

        // A block sequence value may be indented at the same column as its key (the compact style of spec
        // Example 8.22, e.g. "a:\n- 1\n- 2"). A more-indented sequence is handled by the block-node parse below.
        if (col == keyIndent && Peek() == (byte)'-' && IsBlankOrBreakOrEnd(PeekAt(1)))
            return ParseBlockSequence(col, null, null);

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
        string? anchor = TryReadAnchorAndTag(out string? tag);
        SkipSpaces();

        byte c = Peek();
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
            int flow = ParseFlowNodeFromBlock(keyIndent);
            ApplyProperties(flow, anchor, tag);
            SkipLineTrailing();
            return Finish(flow, anchor);
        }

        if (c == (byte)'*')
        {
            if (anchor is not null || tag is not null)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

            int alias = ParseAlias();
            SkipLineTrailing();
            return Finish(alias, anchor);
        }

        if (c == (byte)'|' || c == (byte)'>')
            return Finish(ParseBlockScalar(keyIndent, anchor, tag), anchor);

        int scalar = ParseBlockScalarPlainOrQuoted(keyIndent + 1);
        ApplyProperties(scalar, anchor, tag);
        return Finish(scalar, anchor);
    }

    /// <summary>
    /// Parses a block sequence whose entries begin at the given indentation, counting it toward the nesting depth.
    /// </summary>
    /// <param name="indent">The column at which each <c>-</c> entry marker appears.</param>
    /// <param name="anchor">The anchor captured for the sequence node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the sequence node, or <see langword="null" />.</param>
    /// <returns>The row index of the sequence.</returns>
    /// <exception cref="YamlFormatException">The sequence would nest deeper than the maximum depth.</exception>
    private int ParseBlockSequence(int indent, string? anchor, string? tag)
    {
        EnterCollection();
        try
        {
            return ParseBlockSequenceCore(indent, anchor, tag);
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a block sequence whose entries begin at the given indentation, after the depth guard has been applied.
    /// </summary>
    /// <param name="indent">The column at which each <c>-</c> entry marker appears.</param>
    /// <param name="anchor">The anchor captured for the sequence node, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the sequence node, or <see langword="null" />.</param>
    /// <returns>The row index of the sequence.</returns>
    private int ParseBlockSequenceCore(int indent, string? anchor, string? tag)
    {
        int sequence = NewContainer(YamlReaderNodeKind.Sequence, _pos, anchor, tag);

        while (true)
        {
            SkipBlankCommentLines();
            if (AtStreamEnd() || AtDocumentBoundary())
                break;

            if (CurrentColumn() != indent || Peek() != (byte)'-' || !IsBlankOrBreakOrEnd(PeekAt(1)))
                break;

            Advance(); // consume '-'
            int entryColumn = CurrentColumn();

            bool separationHasTab = false;
            while (Peek() is (byte)' ' or (byte)'\t')
            {
                if (Peek() == (byte)'\t')
                    separationHasTab = true;

                _pos++;
            }

            // A tab cannot indent a compact sequence or mapping that begins on the dash's line.
            if (separationHasTab && AtCompactCollectionStart())
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
                // Content on the same line as the dash; its effective indent is the column after the dash. A block
                // scalar there still takes the dash's column as its parent indentation, so that an indentation
                // indicator, or the detected indentation, counts from the dash.
                element = ParseBlockNode(entryColumn + 1, indent);
            }

            AppendChild(sequence, element);
        }

        return sequence;
    }

    /// <summary>
    /// Reads the text that names a mapping key, for a key node parsed as a node (a flow, flow-pair, or explicit key).
    /// </summary>
    /// <param name="row">The row index of the key node.</param>
    /// <returns>
    /// The scalar's text as written, after quoting and escapes are processed, as an implicit block key is named.
    /// </returns>
    /// <exception cref="YamlFormatException">The key is a sequence, a mapping, or an alias.</exception>
    private string KeyTextOf(int row)
    {
        YamlReaderRow r = _rows[row];

        // A key is named by its text, never by the value the schema resolves the text to, so that 0x10 and 16, or y and
        // yes under YAML 1.1, stay distinct keys in every key form.
        if (r.Kind == YamlReaderNodeKind.Scalar)
            return ScalarText(r);

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
        YamlReaderRow v = _rows[valueRow];
        v.Key = keyText;
        _rows[valueRow] = v;

        if (index.TryGetValue(keyText, out int existing))
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
        int first = _rows[parent].FirstChild;
        if (first == child)
        {
            UnlinkChild(parent, child, -1);
            return;
        }

        int previous = first;
        while (previous >= 0 && _rows[previous].NextSibling != child)
            previous = _rows[previous].NextSibling;

        if (previous >= 0)
            UnlinkChild(parent, child, previous);
    }

    /// <summary>
    /// Enters a sequence or mapping, counting it toward the nesting depth. Only collections count, so a maximum depth
    /// of N admits N nested collections whatever scalar sits at the deepest level.
    /// </summary>
    /// <exception cref="YamlFormatException">The collection would nest deeper than the maximum depth.</exception>
    private void EnterCollection()
    {
        if (++_depth > _maxDepth)
            throw Error(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlNestingTooDeep, _maxDepth));
    }

    /// <summary>
    /// Creates the format exception for a parse error at the current position.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    private YamlFormatException Error(string message) =>
        new(message, _line + 1, CurrentColumn() + 1, _pos);
}
