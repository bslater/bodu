// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeListSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents a Bencode list value (<c>l…e</c>) — an ordered sequence of values.
/// </summary>
public sealed class BencodeListSyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// The list elements, in order.
    /// </summary>
    private readonly SyntaxList<BencodeSyntaxNode> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeListSyntax" /> class.
    /// </summary>
    /// <param name="span">The span the list covers in the source.</param>
    public BencodeListSyntax(SourceSpan span)
        : base(BencodeSyntaxKind.List, span)
    {
        _items = new SyntaxList<BencodeSyntaxNode>(this);
    }

    /// <summary>
    /// Gets the list elements, in order.
    /// </summary>
    /// <returns>The ordered elements.</returns>
    public IReadOnlyList<BencodeSyntaxNode> Items => _items;

    /// <summary>
    /// Appends an element to the list.
    /// </summary>
    /// <param name="node">The element to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public void Add(BencodeSyntaxNode node) => _items.Add(node);

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => _items;

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.Write("l"u8);
        foreach (BencodeSyntaxNode item in _items)
            item.WriteTo(writer);
        writer.Write("e"u8);
    }
}
