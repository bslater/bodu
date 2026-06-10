// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSyntaxNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Serves as the base for every node in a Bencode concrete syntax tree, adding a strongly-typed <see cref="Kind" />
/// over the common <see cref="SyntaxNode.RawKind" /> and the ability to write the node back to its canonical bytes.
/// </summary>
/// <remarks>
/// Because the Bencode grammar is canonical and carries no insignificant text, a parsed tree reproduces its source
/// exactly by re-encoding its values; there is no trivia to preserve. Nodes are therefore not tokens, and the lossless
/// guarantee is expressed through <see cref="WriteTo" /> and <see cref="ToByteArray" /> rather than through token text.
/// </remarks>
public abstract class BencodeSyntaxNode
    : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSyntaxNode" /> class.
    /// </summary>
    /// <param name="kind">The node kind.</param>
    /// <param name="span">The span the node covers in the source.</param>
    private protected BencodeSyntaxNode(BencodeSyntaxKind kind, SourceSpan span)
        : base((int)kind, span)
    {
    }

    /// <summary>
    /// Gets the strongly-typed kind of the node.
    /// </summary>
    /// <returns>The node kind.</returns>
    public BencodeSyntaxKind Kind => (BencodeSyntaxKind)RawKind;

    /// <inheritdoc />
    public override bool IsToken => false;

    /// <summary>
    /// Writes the canonical Bencode encoding of this node to the supplied buffer writer.
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public abstract void WriteTo(IBufferWriter<byte> writer);

    /// <summary>
    /// Encodes this node to a new byte array in canonical Bencode form.
    /// </summary>
    /// <returns>The canonical Bencode bytes.</returns>
    public byte[] ToByteArray()
    {
        ArrayBufferWriter<byte> buffer = new();
        WriteTo(buffer);
        return buffer.WrittenSpan.ToArray();
    }
}
