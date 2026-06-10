// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDictionarySyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Represents a Bencode dictionary value (<c>d…e</c>) — an ordered sequence of key/value pairs whose keys are sorted in
/// ascending bytewise order.
/// </summary>
public sealed class BencodeDictionarySyntax
    : BencodeSyntaxNode
{
    /// <summary>
    /// The dictionary entries, in key order.
    /// </summary>
    private readonly SyntaxList<BencodeKeyValueSyntax> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeDictionarySyntax" /> class.
    /// </summary>
    /// <param name="span">The span the dictionary covers in the source.</param>
    public BencodeDictionarySyntax(SourceSpan span)
        : base(BencodeSyntaxKind.Dictionary, span)
    {
        _entries = new SyntaxList<BencodeKeyValueSyntax>(this);
    }

    /// <summary>
    /// Gets the dictionary entries, in key order.
    /// </summary>
    /// <returns>The ordered entries.</returns>
    public IReadOnlyList<BencodeKeyValueSyntax> Entries => _entries;

    /// <summary>
    /// Appends a key/value pair to the dictionary.
    /// </summary>
    /// <param name="entry">The entry to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entry" /> is <see langword="null" />.
    /// </exception>
    public void Add(BencodeKeyValueSyntax entry) => _entries.Add(entry);

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => _entries;

    /// <inheritdoc />
    public override void WriteTo(IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.Write("d"u8);
        foreach (BencodeKeyValueSyntax entry in _entries)
            entry.WriteTo(writer);
        writer.Write("e"u8);
    }
}
