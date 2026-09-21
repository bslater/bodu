// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines the domain-separation prefix bytes that distinguish a leaf, an internal node, and a length-bound root.
/// </summary>
/// <remarks>
/// <para>
/// One declaration serves every Merkle type in the package, so a leaf hashed under one prefix by one type and a
/// different prefix by another — a silent interoperability break — cannot happen.
/// </para>
/// <para>
/// <strong>The prefixes are not decoration.</strong> Following
/// <see href="https://www.rfc-editor.org/rfc/rfc6962#section-2.1">RFC 6962 §2.1</see>, a leaf is hashed as
/// <c>H(0x00 || data)</c> and an internal node as <c>H(0x01 || children)</c>. Without the leaf prefix a one-leaf tree's
/// root would be the entry's bare digest, and an internal node's preimage — a concatenation of child hashes — could be
/// presented as leaf data. That is how a second tree is constructed to produce a root somebody has already signed. The
/// third prefix keeps a length-bound root from being confused with either.
/// </para>
/// <para>
/// <strong>One tree, shared.</strong> RFC 6962 reduces a tree of <em>n</em> leaves by splitting at <c>k</c>, the
/// largest power of two strictly below <em>n</em>, and promoting a lone subtree root unchanged. The hashers in
/// <c>Bodu.Security.Cryptography</c> fold leaves level by level through the shared <c>MerkleLevelFold</c>, which at its
/// default fan-out of two visits exactly the nodes of that recursion, so their roots are RFC 6962's and
/// <c>Rfc6962MerkleTree</c>'s proofs verify against them. A wider fan-out is an explicit non-RFC mode of the same fold.
/// For inclusion and consistency proofs, use <c>Rfc6962MerkleTree</c>.
/// </para>
/// <para>
/// The final partial leaf is hashed at its actual byte length rather than being zero-padded to the block size in both
/// libraries, so the exact input length is bound into every leaf and trailing-zero variations cannot collide.
/// </para>
/// </remarks>
internal static class MerkleTreeFormat
{
    /// <summary>The domain-separation prefix byte prepended to leaf data before hashing.</summary>
    internal const byte LeafPrefix = 0x00;

    /// <summary>The domain-separation prefix byte prepended to an internal node's concatenated child hashes.</summary>
    internal const byte InternalNodePrefix = 0x01;

    /// <summary>The domain-separation prefix byte prepended to a length-bound root's big-endian value and tree head, as <c>H(0x02 || u64_be(boundValue) || treeHead)</c>.</summary>
    /// <remarks>
    /// Used only by <c>Rfc6962MerkleTree</c>'s bound-root mode, which is an addition to RFC 6962 rather than part of
    /// it. The <c>Bodu.Security.Cryptography</c> hashers compile this constant without using it.
    /// </remarks>
    internal const byte RootPrefix = 0x02;
}
