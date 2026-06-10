// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeKeyValueSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents a single key/value pair within a Bencode dictionary, pairing a byte-string key with its value.
/// </summary>
public sealed class BencodeKeyValueSyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeKeyValueSyntax" /> class.
    /// </summary>
    /// <param name="key">The byte-string key.</param>
    /// <param name="value">The value.</param>
    /// <param name="span">The span the pair covers in the source.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public BencodeKeyValueSyntax(BencodeByteStringSyntax key, BencodeSyntaxNode value, SourceSpan span)
        : base(BencodeSyntaxKind.KeyValuePair, span)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);

        Key = key;
        Value = value;
        AttachChildren(key, value);
    }

    /// <summary>
    /// Gets the byte-string key.
    /// </summary>
    /// <returns>The key.</returns>
    public BencodeByteStringSyntax Key { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <returns>The value.</returns>
    public BencodeSyntaxNode Value { get; }

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens()
    {
        yield return Key;
        yield return Value;
    }

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);
        Key.WriteTo(writer);
        Value.WriteTo(writer);
    }
}
