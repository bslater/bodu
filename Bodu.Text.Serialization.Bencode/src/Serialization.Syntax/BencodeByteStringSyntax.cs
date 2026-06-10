// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeByteStringSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents a Bencode byte-string value (<c>&lt;length&gt;:&lt;bytes&gt;</c>). The bytes are binary; the
/// <see cref="AsString" /> accessor decodes them as UTF-8 for the common case of textual keys and values.
/// </summary>
public sealed class BencodeByteStringSyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeByteStringSyntax" /> class.
    /// </summary>
    /// <param name="value">The raw bytes of the string.</param>
    /// <param name="span">The span the value covers in the source.</param>
    public BencodeByteStringSyntax(ReadOnlyMemory<byte> value, SourceSpan span)
        : base(BencodeSyntaxKind.ByteString, span)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw bytes of the string.
    /// </summary>
    /// <returns>The byte content.</returns>
    public ReadOnlyMemory<byte> Value { get; }

    /// <summary>
    /// Decodes the bytes as UTF-8 text.
    /// </summary>
    /// <returns>The UTF-8 decoding of the byte content.</returns>
    public string AsString() => Encoding.UTF8.GetString(Value.Span);

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => [];

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);
        BencodeEncoding.WriteByteString(writer, Value.Span);
    }
}
