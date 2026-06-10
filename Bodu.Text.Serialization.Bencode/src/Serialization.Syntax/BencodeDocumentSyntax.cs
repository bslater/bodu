// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents the root of a parsed Bencode document, wrapping the single top-level value.
/// </summary>
public sealed class BencodeDocumentSyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeDocumentSyntax" /> class.
    /// </summary>
    /// <param name="root">The single top-level value.</param>
    /// <param name="span">The span the document covers in the source.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="root" /> is <see langword="null" />.
    /// </exception>
    public BencodeDocumentSyntax(BencodeSyntaxNode root, SourceSpan span)
        : base(BencodeSyntaxKind.Document, span)
    {
        ThrowHelper.ThrowIfNull(root);

        Root = root;
        AttachChildren(root);
    }

    /// <summary>
    /// Gets the single top-level value of the document.
    /// </summary>
    /// <returns>The root value.</returns>
    public BencodeSyntaxNode Root { get; }

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens()
    {
        yield return Root;
    }

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);
        Root.WriteTo(writer);
    }
}
