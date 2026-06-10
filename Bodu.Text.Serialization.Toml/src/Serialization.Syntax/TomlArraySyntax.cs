// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArraySyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Represents a TOML array — an ordered, possibly heterogeneous sequence of values.
/// </summary>
public sealed class TomlArraySyntax
    : TomlSyntaxNode
{
    /// <summary>
    /// The array elements, in order.
    /// </summary>
    private readonly SyntaxList<TomlSyntaxNode> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArraySyntax" /> class.
    /// </summary>
    /// <param name="span">The span the array covers in the source.</param>
    public TomlArraySyntax(SourceSpan span = default)
        : base(TomlSyntaxKind.Array, span)
    {
        _items = new SyntaxList<TomlSyntaxNode>(this);
    }

    /// <summary>
    /// Gets the array elements, in order.
    /// </summary>
    /// <returns>The ordered elements.</returns>
    public IReadOnlyList<TomlSyntaxNode> Items => _items;

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    /// <returns>The element count.</returns>
    public int Count => _items.Count;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The element index.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public TomlSyntaxNode this[int index] => _items[index];

    /// <summary>
    /// Appends an element to the array.
    /// </summary>
    /// <param name="node">The element to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public void Add(TomlSyntaxNode node) => _items.Add(node);

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => _items;
}
