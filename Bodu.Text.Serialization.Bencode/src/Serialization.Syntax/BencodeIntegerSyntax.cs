// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeIntegerSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents a Bencode integer value (<c>i&lt;value&gt;e</c>), stored as a 64-bit signed integer.
/// </summary>
public sealed class BencodeIntegerSyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeIntegerSyntax" /> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <param name="span">The span the value covers in the source.</param>
    public BencodeIntegerSyntax(long value, SourceSpan span)
        : base(BencodeSyntaxKind.Integer, span)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    /// <returns>The 64-bit signed integer.</returns>
    public long Value { get; }

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => [];

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);
        BencodeEncoding.WriteInteger(writer, Value);
    }
}
