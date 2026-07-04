// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines the shared wire-format constants for the Merkle tree hash implementations, ensuring
/// <see cref="MerkleTreeHash" /> and <see cref="ParallelMerkleTreeHash" /> encode leaves and internal nodes
/// identically.
/// </summary>
/// <remarks>
/// <para>
/// Both implementations follow the RFC 6962 §2.1 domain-separation scheme: a leaf is hashed as
/// <c>H(<see cref="LeafPrefix" /> || data)</c> and an internal node as
/// <c>H(<see cref="InternalNodePrefix" /> || child_0 || … || child_{k-1})</c>. The distinct prefixes prevent an
/// internal node's concatenated child hashes from being replayed as leaf data (second-preimage / node-confusion
/// resistance).
/// </para>
/// <para>
/// The final partial leaf is hashed at its actual byte length rather than being zero-padded to the block size, so
/// the exact input length is bound into every leaf and trailing-zero variations cannot collide.
/// </para>
/// </remarks>
internal static class MerkleTreeFormat
{
    /// <summary>The domain-separation prefix byte prepended to leaf-block data before hashing.</summary>
    internal const byte LeafPrefix = 0x00;

    /// <summary>The domain-separation prefix byte prepended to an internal node's concatenated child hashes.</summary>
    internal const byte InternalNodePrefix = 0x01;
}
