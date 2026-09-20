// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Computes the Merkle Tree Hash exactly as
/// <see href="https://www.rfc-editor.org/rfc/rfc6962#section-2.1">RFC 6962 §2.1</see> defines it, over an ordered
/// sequence of entries or over fixed-size blocks of a byte stream.
/// </summary>
/// <remarks>
/// <para>
/// A tree of <em>n</em> entries is reduced by splitting at <c>k</c>, the largest power of two <em>strictly</em> below
/// <em>n</em>, and recursing: the left subtree is always perfect and the right holds the remainder. A subtree root is
/// promoted unchanged, never re-hashed. Three entries therefore split 2 + 1 and never 1 + 2, and there is no fan-out to
/// configure — RFC 6962 is binary.
/// </para>
/// <para>
/// Leaves and internal nodes are domain-separated: <c>leaf(d) = H(0x00 || d)</c> and
/// <c>node(l, r) = H(0x01 || l || r)</c>. Without the leaf prefix a one-entry tree's root would be the entry's bare
/// digest, and an internal node's preimage — two concatenated child hashes — could be presented as leaf data. That is
/// how a second tree is constructed to produce a root somebody has already signed.
/// </para>
/// <para>
/// An empty tree's root is <c>H()</c>, the hash of zero bytes; no exception is raised. A log that has published nothing
/// still has a head to sign.
/// </para>
/// <para>
/// <strong>Relationship to the other Merkle types.</strong> <c>MerkleTreeHash</c> and <c>ParallelMerkleTreeHash</c>
/// share this type's domain separation but reduce level by level with a configurable fan-out, re-hashing a lone
/// leftover child as a one-child node. Their roots agree with this type's only when the leaf count is a power of two.
/// This type is the one to use when a root must interoperate with a transparency log or any other RFC 6962
/// implementation; the other two are unchanged and their roots are stable.
/// </para>
/// <para>
/// <strong>Thread safety.</strong> Instances are immutable and every operation is safe for concurrent use, provided the
/// supplied factory returns a <em>fresh</em> <see cref="HashAlgorithm" /> on each call — as <c>SHA256.Create</c> does.
/// A factory that hands back one shared instance is not safe and will corrupt concurrent computations. A single
/// instance may therefore be registered as a singleton and used from many connection handlers at once.
/// </para>
/// <para>
/// <strong>Algorithm agnosticism.</strong> Any <see cref="HashAlgorithm" /> may be configured; nothing assumes a
/// 32-byte digest. The digest width is discovered once at construction and exposed as <see cref="HashLength" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Collections.Merkle;
///
/// var tree = new Rfc6962MerkleTree(SHA256.Create);
///
/// ReadOnlyMemory<byte>[] entries = [ new byte[0], new byte[] { 0x00 }, new byte[] { 0x10 } ];
/// byte[] root = tree.ComputeRoot(entries);
///]]>
/// </code>
/// </example>
/// <seealso cref="MerkleComputation" /> <seealso cref="MerkleBlocks" />
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>The factory invoked once per operation to obtain a hash algorithm instance.</summary>
    private readonly Func<HashAlgorithm> _algorithmFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rfc6962MerkleTree" /> class using the specified hash algorithm
    /// factory delegate.
    /// </summary>
    /// <param name="algorithmFactory">
    /// A delegate supplying the <see cref="HashAlgorithm" /> used for every leaf, node and root. Must not be
    /// <see langword="null" /> and must return a fresh instance on each call.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The factory returned <see langword="null" />, or an algorithm whose digest length is not positive.
    /// </exception>
    /// <remarks>
    /// The factory is invoked once here to establish <see cref="HashLength" />, so a factory that cannot produce an
    /// algorithm fails at construction rather than at the first computation.
    /// </remarks>
    public Rfc6962MerkleTree(Func<HashAlgorithm> algorithmFactory)
    {
        ThrowHelper.ThrowIfNull(algorithmFactory);

        _algorithmFactory = algorithmFactory;

        using HashAlgorithm probe = algorithmFactory()
            ?? throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_MerkleAlgorithmFactoryNull, nameof(algorithmFactory));

        int hashLength = probe.HashSize / 8;
        if (hashLength <= 0)
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_MerkleHashLengthNotPositive, nameof(algorithmFactory));

        HashLength = hashLength;
    }

    /// <summary>
    /// Gets the length, in bytes, of every leaf hash, node hash and root this instance produces.
    /// </summary>
    /// <value>The configured algorithm's digest length in bytes — 32 for SHA-256, 64 for SHA-512.</value>
    public int HashLength { get; }

    /// <summary>
    /// Returns the largest power of two strictly less than <paramref name="count" /> — RFC 6962's split point.
    /// </summary>
    /// <param name="count">The number of entries in the subtree being split. Must be greater than one.</param>
    /// <returns>The number of entries belonging to the perfect left subtree.</returns>
    /// <remarks>
    /// This is deliberately not a halving. For seven entries the split is 4 + 3, not 3 + 4 or 4 + 4; a tree built by
    /// halving has the same leaves and a different root.
    /// </remarks>
    private static int SplitPoint(int count) => MerkleTreeCore.SplitPoint(count);

    /// <summary>
    /// Computes <c>H(prefix || first || second)</c> using the supplied algorithm and a single pooled buffer.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="prefix">The domain-separation prefix byte.</param>
    /// <param name="first">The first payload segment.</param>
    /// <param name="second">The second payload segment, empty when the payload has only one.</param>
    /// <returns>The resulting hash, exactly <see cref="HashLength" /> bytes.</returns>
    /// <remarks>
    /// The one-shot <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" /> resets the
    /// algorithm's state on every call, so one instance serves an entire computation without per-node allocation.
    /// </remarks>
    private byte[] HashWithPrefix(
        HashAlgorithm hasher,
        byte prefix,
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second = default) =>
        MerkleTreeCore.HashWithPrefix(hasher, HashLength, prefix, first, second);
}
