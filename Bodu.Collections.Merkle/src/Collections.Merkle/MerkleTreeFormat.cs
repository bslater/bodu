// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Merkle;

/// <summary>
/// Defines the domain-separation prefix bytes that distinguish a leaf, an internal node and a length-bound root.
/// </summary>
/// <remarks>
/// <para>
/// The first two prefixes are <see href="https://www.rfc-editor.org/rfc/rfc6962#section-2.1">RFC 6962 §2.1</see>'s and
/// are not decoration: without the leaf prefix a one-entry tree's root would be the entry's bare digest, and an
/// internal node's preimage — two concatenated child hashes — could be presented as leaf data. That is how a second
/// tree is constructed to produce a root somebody has already signed. The third prefix keeps a length-bound root from
/// being confused with either.
/// </para>
/// <para>
/// <strong>Why these values are declared here as well as in <c>Bodu.Security.Cryptography</c>.</strong> That
/// library's <c>MerkleTreeHash</c> and <c>ParallelMerkleTreeHash</c> use the same leaf and node prefixes over a
/// different tree shape. Declaring the values independently is what keeps this package free of a dependency on the
/// cryptography library, and they cannot silently drift apart: each side pins its published roots as test
/// expectations, so changing a prefix on either side fails that side's vectors immediately.
/// </para>
/// </remarks>
internal static class MerkleTreeFormat
{
    /// <summary>The prefix byte prepended to an entry's bytes before hashing it as a leaf.</summary>
    internal const byte LeafPrefix = 0x00;

    /// <summary>The prefix byte prepended to an internal node's concatenated child hashes.</summary>
    internal const byte InternalNodePrefix = 0x01;

    /// <summary>The prefix byte prepended to a length-bound root's big-endian value and tree head.</summary>
    internal const byte RootPrefix = 0x02;
}
