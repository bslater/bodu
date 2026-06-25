// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Represents a parsed, read-only YAML document as an immutable tree of <see cref="YamlElement" /> views over a flat,
/// index-linked node store, in the manner of <see cref="System.Text.Json.JsonDocument" />.
/// </summary>
/// <remarks>
/// The document materializes a single YAML document from its source. Element views are lightweight structs that remain
/// valid until the document is disposed. The type is not thread-safe for concurrent disposal with reads.
/// </remarks>
public sealed partial class YamlDocument : IDisposable
{
    private List<YamlReaderRow>? _rows;
    private string[] _strings;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlDocument" /> class over a parsed row store.
    /// </summary>
    /// <param name="rows">The flat node store, with the root at index zero.</param>
    /// <param name="strings">The decoded-string side table referenced by string scalar rows.</param>
    private YamlDocument(List<YamlReaderRow> rows, string[] strings)
    {
        _rows = rows;
        _strings = strings;
    }

    /// <summary>
    /// Gets the root element of the document.
    /// </summary>
    /// <value>The element view onto the document root.</value>
    public YamlElement RootElement => new(this, 0);

    /// <summary>
    /// Parses a YAML document from UTF-8 source.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static YamlDocument Parse(ReadOnlySpan<byte> utf8Yaml) =>
        Parse(utf8Yaml, default);

    /// <summary>
    /// Parses a YAML document from UTF-8 source using the specified options.
    /// </summary>
    /// <param name="utf8Yaml">The UTF-8 encoded YAML source.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static YamlDocument Parse(ReadOnlySpan<byte> utf8Yaml, YamlDocumentOptions options)
    {
        var buffer = utf8Yaml.ToArray();
        var parser = new YamlParser(buffer, buffer.Length, options.SpecVersion, options.EffectiveMaxDepth, options.DuplicateKeyBehavior);
        var rows = parser.Parse();
        return new YamlDocument(rows, parser.Strings.ToArray());
    }

    /// <summary>
    /// Parses a YAML document from a string.
    /// </summary>
    /// <param name="yaml">The YAML source text.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static YamlDocument Parse(string yaml) =>
        Parse(yaml, default);

    /// <summary>
    /// Parses a YAML document from a string using the specified options.
    /// </summary>
    /// <param name="yaml">The YAML source text.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static YamlDocument Parse(string yaml, YamlDocumentOptions options)
    {
        Bodu.ThrowHelper.ThrowIfNull(yaml);
        return Parse(Encoding.UTF8.GetBytes(yaml), options);
    }

    /// <summary>
    /// Releases the resources held by the document and invalidates its element views.
    /// </summary>
    public void Dispose()
    {
        _rows = null;
        _strings = [];
    }

    /// <summary>
    /// Resolves an alias row to its target, or returns the index unchanged for non-alias rows.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The resolved row index.</returns>
    internal int Resolve(int index)
    {
        var rows = Rows;
        var r = rows[index];
        return r.Kind == YamlReaderNodeKind.Alias && r.AliasTarget >= 0 ? r.AliasTarget : index;
    }

    /// <summary>
    /// Gets the value kind of the node at the specified row.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The value kind.</returns>
    internal YamlValueKind GetKind(int index)
    {
        var rows = Rows;
        var r = rows[Resolve(index)];
        return r.Kind switch
        {
            YamlReaderNodeKind.Sequence => YamlValueKind.Sequence,
            YamlReaderNodeKind.Mapping => YamlValueKind.Mapping,
            YamlReaderNodeKind.Scalar => r.ValueKind,
            _ => YamlValueKind.Null,
        };
    }

    /// <summary>
    /// Gets the presentation style of a scalar node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The scalar style.</returns>
    internal YamlScalarStyle GetStyle(int index) => Rows[Resolve(index)].ScalarStyle;

    /// <summary>
    /// Gets the string value of a string scalar node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a string scalar.</exception>
    internal string GetString(int index)
    {
        var r = Rows[Resolve(index)];
        if (r.Kind != YamlReaderNodeKind.Scalar || r.ValueKind != YamlValueKind.String)
            throw new InvalidOperationException();

        return _strings[(int)r.ScalarBits];
    }

    /// <summary>
    /// Gets the integer value of an integer scalar node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">The node is not an integer scalar.</exception>
    internal long GetInt64(int index)
    {
        var r = Rows[Resolve(index)];
        if (r.ValueKind != YamlValueKind.Integer)
            throw new InvalidOperationException();

        return r.AsInt64();
    }

    /// <summary>
    /// Gets the floating-point value of a numeric scalar node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The floating-point value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a numeric scalar.</exception>
    internal double GetDouble(int index)
    {
        var r = Rows[Resolve(index)];
        return r.ValueKind switch
        {
            YamlValueKind.Float => r.AsDouble(),
            YamlValueKind.Integer => r.AsInt64(),
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// Gets the boolean value of a boolean scalar node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a boolean scalar.</exception>
    internal bool GetBoolean(int index)
    {
        var r = Rows[Resolve(index)];
        if (r.ValueKind != YamlValueKind.Boolean)
            throw new InvalidOperationException();

        return r.AsBoolean();
    }

    /// <summary>
    /// Gets the number of elements in a sequence node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The element count.</returns>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    internal int GetSequenceLength(int index)
    {
        var r = Rows[Resolve(index)];
        if (r.Kind != YamlReaderNodeKind.Sequence)
            throw new InvalidOperationException();

        return r.ChildCount;
    }

    /// <summary>
    /// Gets the row index of the first child of a container node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The first child row index, or <c>-1</c> when empty.</returns>
    internal int FirstChild(int index) => Rows[Resolve(index)].FirstChild;

    /// <summary>
    /// Gets the row index of the next sibling of a node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The next sibling row index, or <c>-1</c> when last.</returns>
    internal int NextSibling(int index) => Rows[index].NextSibling;

    /// <summary>
    /// Gets the number of key/value pairs in a mapping node.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The pair count.</returns>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    internal int GetMappingCount(int index)
    {
        var r = Rows[Resolve(index)];
        if (r.Kind != YamlReaderNodeKind.Mapping)
            throw new InvalidOperationException();

        return r.ChildCount;
    }

    /// <summary>
    /// Gets the decoded key of a mapping value row.
    /// </summary>
    /// <param name="index">The row index of a mapping value.</param>
    /// <returns>The key text, or the empty string when none.</returns>
    internal string GetKey(int index) => Rows[index].Key ?? string.Empty;

    /// <summary>
    /// Gets the row index of a sequence element by position.
    /// </summary>
    /// <param name="index">The sequence row index.</param>
    /// <param name="elementIndex">The zero-based element position.</param>
    /// <returns>The element row index.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elementIndex" /> is out of range.</exception>
    internal int GetSequenceElement(int index, int elementIndex)
    {
        if (elementIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        var child = FirstChild(index);
        var i = 0;
        while (child >= 0)
        {
            if (i == elementIndex)
                return child;

            child = NextSibling(child);
            i++;
        }

        throw new ArgumentOutOfRangeException(nameof(elementIndex));
    }

    /// <summary>
    /// Attempts to find a mapping value by key.
    /// </summary>
    /// <param name="index">The mapping row index.</param>
    /// <param name="name">The key to find.</param>
    /// <param name="valueRow">When the method returns <see langword="true" />, the value row index.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    internal bool TryGetProperty(int index, string name, out int valueRow)
    {
        var child = FirstChild(index);
        while (child >= 0)
        {
            if (string.Equals(Rows[child].Key, name, StringComparison.Ordinal))
            {
                valueRow = child;
                return true;
            }

            child = NextSibling(child);
        }

        valueRow = -1;
        return false;
    }

    /// <summary>
    /// Gets the backing row store, throwing when the document has been disposed.
    /// </summary>
    /// <value>The row store.</value>
    /// <exception cref="ObjectDisposedException">The document has been disposed.</exception>
    internal List<YamlReaderRow> Rows =>
        _rows ?? throw new ObjectDisposedException(nameof(YamlDocument));
}
