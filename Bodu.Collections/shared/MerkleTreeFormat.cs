// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if SECURITY_CRYPTOGRAPHY
namespace Bodu.Security.Cryptography;
#else
namespace Bodu.Collections.Specialized;
#endif

/// <summary>
/// Defines the domain-separation prefix bytes that distinguish a leaf, an internal node, and a length-bound root.
/// </summary>
/// <remarks>
/// <para>
/// This file lives in <c>Bodu.Collections/shared/</c> and is compiled into more than one assembly: into
/// <c>Bodu.Collections</c>, whose <c>Rfc6962MerkleTree</c> uses all three prefixes, and source-compiled (no package
/// dependency) into <c>Bodu.Security.Cryptography</c>, whose <c>MerkleTreeHash</c> and <c>ParallelMerkleTreeHash</c>
/// use the first two. The consuming project selects the namespace with the <c>SECURITY_CRYPTOGRAPHY</c> preprocessor
/// symbol, following the <c>Bodu.IO.Hashing/shared</c> pattern.
/// </para>
/// <para>
/// Sharing one declaration is what makes prefix drift structurally impossible rather than merely test-detected. The two
/// tree shapes are <em>not</em> shared and deliberately so — see below — but a leaf hashed under one prefix in one
/// library and a different prefix in the other would be a silent interoperability break, and that can no longer happen.
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
/// <strong>Domain separation only — not one shared tree.</strong> What the two libraries share is this prefix scheme
/// and nothing more. RFC 6962 reduces a tree of <em>n</em> leaves by splitting at <c>k</c>, the largest power of two
/// strictly below <em>n</em>, and promoting a lone subtree root unchanged; the level-by-level types in
/// <c>Bodu.Security.Cryptography</c> reduce level by level with a configurable fan-out and re-hash a lone leftover
/// child as a one-child node. Their roots agree only when the leaf count is a power of two, so a root from those types
/// must not be cross-checked against a transparency log. For RFC 6962's actual tree, and for inclusion and consistency
/// proofs, use <c>Rfc6962MerkleTree</c> in the <c>Bodu.Collections</c> package.
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
    /// it. The level-by-level types compile this constant without using it.
    /// </remarks>
    internal const byte RootPrefix = 0x02;
}
