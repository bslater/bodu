// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Represents a single node within a <see cref="YamlDocument" /> as a lightweight, read-only view, in the manner of
/// <see cref="System.Text.Json.JsonElement" />.
/// </summary>
/// <remarks>
/// An element is a small value type that refers back to its owning document and a node index. It is valid only while
/// the owning document has not been disposed.
/// </remarks>
public readonly partial struct YamlElement
{
    private readonly YamlDocument _document;
    private readonly int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="index">The node row index within the document.</param>
    internal YamlElement(YamlDocument document, int index)
    {
        _document = document;
        _index = index;
    }

    /// <summary>
    /// Gets the value kind of the node.
    /// </summary>
    /// <value>The resolved value kind.</value>
    public YamlValueKind ValueKind => _document.GetKind(_index);

    /// <summary>
    /// Gets the presentation style of the node when it is a scalar.
    /// </summary>
    /// <value>The scalar style, or <see cref="YamlScalarStyle.Any" /> for non-scalar nodes.</value>
    public YamlScalarStyle ScalarStyle => _document.GetStyle(_index);

    /// <summary>
    /// Gets the element at the specified position within a sequence node.
    /// </summary>
    /// <param name="index">The zero-based element position.</param>
    /// <value>The element view at the position.</value>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    public YamlElement this[int index]
    {
        get
        {
            if (ValueKind != YamlValueKind.Sequence)
                throw new InvalidOperationException();

            return new YamlElement(_document, _document.GetSequenceElement(_document.Resolve(_index), index));
        }
    }

    /// <summary>
    /// Returns the string value of a string scalar node.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a string scalar.</exception>
    public string GetString() => _document.GetString(_index);

    /// <summary>
    /// Returns the integer value of an integer scalar node.
    /// </summary>
    /// <returns>The 64-bit integer value.</returns>
    /// <exception cref="InvalidOperationException">The node is not an integer scalar.</exception>
    public long GetInt64() => _document.GetInt64(_index);

    /// <summary>
    /// Returns the floating-point value of a numeric scalar node.
    /// </summary>
    /// <returns>The double-precision value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a numeric scalar.</exception>
    public double GetDouble() => _document.GetDouble(_index);

    /// <summary>
    /// Returns the boolean value of a boolean scalar node.
    /// </summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a boolean scalar.</exception>
    public bool GetBoolean() => _document.GetBoolean(_index);

    /// <summary>
    /// Returns the number of elements in a sequence node.
    /// </summary>
    /// <returns>The element count.</returns>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    public int GetSequenceLength() => _document.GetSequenceLength(_document.Resolve(_index));

    /// <summary>
    /// Returns the value associated with a key in a mapping node.
    /// </summary>
    /// <param name="propertyName">The key to look up.</param>
    /// <returns>The element view of the mapped value.</returns>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    /// <exception cref="KeyNotFoundException">The key is not present.</exception>
    public YamlElement GetProperty(string propertyName)
    {
        if (!TryGetProperty(propertyName, out YamlElement value))
            throw new KeyNotFoundException();

        return value;
    }

    /// <summary>
    /// Attempts to return the value associated with a key in a mapping node.
    /// </summary>
    /// <param name="propertyName">The key to look up.</param>
    /// <param name="value">When the method returns <see langword="true" />, the mapped value.</param>
    /// <returns><see langword="true" /> when the key is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    public bool TryGetProperty(string propertyName, out YamlElement value)
    {
        Bodu.ThrowHelper.ThrowIfNull(propertyName);
        if (ValueKind != YamlValueKind.Mapping)
            throw new InvalidOperationException();

        if (_document.TryGetProperty(_document.Resolve(_index), propertyName, out int row))
        {
            value = new YamlElement(_document, row);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns an enumerator over the elements of a sequence node.
    /// </summary>
    /// <returns>A sequence enumerator.</returns>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    public SequenceEnumerator EnumerateSequence()
    {
        if (ValueKind != YamlValueKind.Sequence)
            throw new InvalidOperationException();

        return new SequenceEnumerator(_document, _document.Resolve(_index));
    }

    /// <summary>
    /// Returns an enumerator over the key/value pairs of a mapping node.
    /// </summary>
    /// <returns>A mapping enumerator.</returns>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    public MappingEnumerator EnumerateMapping()
    {
        if (ValueKind != YamlValueKind.Mapping)
            throw new InvalidOperationException();

        return new MappingEnumerator(_document, _document.Resolve(_index));
    }

    /// <summary>
    /// Returns a textual representation of the element's value.
    /// </summary>
    /// <returns>The decoded scalar value, or the kind name for containers.</returns>
    public override string ToString() => ValueKind switch
    {
        YamlValueKind.String => GetString(),
        YamlValueKind.Integer => GetInt64().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Float => GetDouble().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Boolean => GetBoolean() ? "true" : "false",
        YamlValueKind.Null => "null",
        _ => ValueKind.ToString(),
    };
}
