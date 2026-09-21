// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the Merkle Tree Hash exactly as
/// <see href="https://www.rfc-editor.org/rfc/rfc6962#section-2.1">RFC 6962 §2.1</see> defines it, over an ordered
/// sequence of entries or over fixed-size blocks of a byte stream, and produces and verifies the inclusion and
/// consistency proofs that go with it.
/// </summary>
/// <remarks>
/// <para>
/// A tree of <em>n</em> entries is reduced by splitting at <c>k</c>, the largest power of two <em>strictly</em> below
/// <em>n</em>, and recursing: the left subtree is always perfect and the right holds the remainder. A subtree root is
/// promoted unchanged, never re-hashed. Three entries therefore split 2 + 1 and never 1 + 2. Leaves are folded level by
/// level with that promotion rule, which visits exactly the nodes of the recursive definition, so a streamed root and a
/// root over a list of entries are the same root.
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
/// <strong>Fan-out.</strong> The default <c>fanOut</c> of two is RFC 6962's tree, the only shape that interoperates
/// with a transparency log or any other implementation of the standard, and the only shape the proof members work over.
/// A wider fan-out hashes that many children into each parent — a shallower, sound commitment of the package's own,
/// offered as an explicit non-RFC mode. On such an instance every root computation works and <see cref="BindRoot" />
/// works, but the authentication-path, consistency-proof and verify members throw <see cref="NotSupportedException" />,
/// because an RFC 6962 proof has no meaning over a tree the standard does not define.
/// </para>
/// <para>
/// <strong>Parallelism.</strong> Leaf hashing is where a block-mode computation spends essentially all of its time —
/// one hash over <c>blockSize</c> bytes per leaf against a handful of digest-sized node hashes — so it is the only part
/// worth spreading across cores. <c>maxDegreeOfParallelism</c> of one, the default, hashes every leaf on the calling
/// thread with one algorithm; <c>-1</c> or a count of two or more hashes leaves in batches with one algorithm per
/// worker, obtained from the factory, and folds them in order on the calling thread. The tree shape never changes with
/// the setting, so a parallel instance and a sequential instance produce the same roots and the same proofs.
/// </para>
/// <para>
/// <strong>Thread safety.</strong> Instances are immutable and every operation is safe for concurrent use, provided the
/// supplied factory returns a <em>fresh</em> <see cref="HashAlgorithm" /> on each call — as <c>SHA256.Create</c> does.
/// A factory that hands back one shared instance is not safe and will corrupt concurrent computations, and cannot serve
/// a parallel instance at all. A single instance may therefore be registered as a singleton and used from many
/// connection handlers at once.
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
/// using Bodu.Security.Cryptography;
///
/// var tree = new MerkleTree(SHA256.Create);
///
/// ReadOnlyMemory<byte>[] entries = [ new byte[0], new byte[] { 0x00 }, new byte[] { 0x10 } ];
/// byte[] root = tree.ComputeRoot(entries);
/// byte[][] path = tree.AuthenticationPath(entries, leafIndex: 2);
///
/// // Large inputs: the same tree, leaves hashed on every core.
/// var parallel = new MerkleTree(SHA256.Create, maxDegreeOfParallelism: -1);
/// byte[] sameRoot = parallel.ComputeRootOfBlocks(File.OpenRead(path), blockSize: 1 << 20);
///]]>
/// </code>
/// </example>
/// <seealso cref="MerkleBlockComputation" /> <seealso cref="MerkleBlockAccumulator" /> <seealso cref="MerkleBlocks" />
public sealed partial class MerkleTree
{
    /// <summary>The fan-out at which the tree is RFC 6962's.</summary>
    private const int BinaryFanOut = 2;

    /// <summary>The factory invoked once per operation, and once per parallel worker, to obtain a hash algorithm.</summary>
    private readonly Func<HashAlgorithm> _algorithmFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTree" /> class using the specified hash algorithm factory
    /// delegate, fan-out and degree of parallelism.
    /// </summary>
    /// <param name="algorithmFactory">
    /// A delegate supplying the <see cref="HashAlgorithm" /> used for every leaf, node and root. Must not be
    /// <see langword="null" /> and must return a fresh instance on each call.
    /// </param>
    /// <param name="fanOut">
    /// The number of children hashed into each parent. Two, the default, is RFC 6962's tree; a larger value is the
    /// package's own non-RFC mode, on which the proof members are unavailable.
    /// </param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of leaves to hash concurrently: one, the default, hashes on the calling thread; <c>-1</c>
    /// uses the processor count; any larger count bounds the workers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The factory returned <see langword="null" />, or an algorithm whose digest length is not positive.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fanOut" /> is less than two, or <paramref name="maxDegreeOfParallelism" /> is zero or below
    /// <c>-1</c>.
    /// </exception>
    /// <remarks>
    /// The factory is invoked once here to establish <see cref="HashLength" />, so a factory that cannot produce an
    /// algorithm fails at construction rather than at the first computation.
    /// </remarks>
    public MerkleTree(Func<HashAlgorithm> algorithmFactory, int fanOut = BinaryFanOut, int maxDegreeOfParallelism = 1)
    {
        ThrowHelper.ThrowIfNull(algorithmFactory);
        if (fanOut < BinaryFanOut) throw new ArgumentOutOfRangeException(nameof(fanOut), CryptoResourceStrings.Arg_OutOfRange_FanOutMinimum);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        _algorithmFactory = algorithmFactory;
        FanOut = fanOut;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;

        using HashAlgorithm probe = algorithmFactory()
            ?? throw new ArgumentException(CryptoResourceStrings.Arg_Invalid_MerkleAlgorithmFactoryNull, nameof(algorithmFactory));

        int hashLength = probe.HashSize / 8;
        if (hashLength <= 0)
            throw new ArgumentException(CryptoResourceStrings.Arg_Invalid_MerkleHashLengthNotPositive, nameof(algorithmFactory));

        HashLength = hashLength;
    }

    /// <summary>
    /// Gets the length, in bytes, of every leaf hash, node hash and root this instance produces.
    /// </summary>
    /// <value>The configured algorithm's digest length in bytes — 32 for SHA-256, 64 for SHA-512.</value>
    public int HashLength { get; }

    /// <summary>
    /// Gets the number of children hashed into each parent.
    /// </summary>
    /// <value>Two for RFC 6962's tree; a larger value for the package's own non-RFC mode.</value>
    public int FanOut { get; }

    /// <summary>
    /// Gets the greatest number of leaves hashed concurrently.
    /// </summary>
    /// <value>
    /// One when leaves are hashed on the calling thread; <c>-1</c> for the processor count; otherwise the bound.
    /// </value>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>
    /// Gets a value indicating whether this instance builds RFC 6962's binary tree, on which the proof members are
    /// available.
    /// </summary>
    public bool IsBinary => FanOut == BinaryFanOut;

    /// <summary>
    /// Gets a value indicating whether leaves are hashed by parallel workers rather than on the calling thread.
    /// </summary>
    private bool IsParallel => MaxDegreeOfParallelism != 1;

    /// <summary>
    /// Folds ordered leaf hashes into the root through the level fold, reporting to the optional recorder.
    /// </summary>
    /// <param name="leafHashes">The leaf hashes, in order.</param>
    /// <param name="hasher">The algorithm to hash nodes with.</param>
    /// <param name="diagnostics">The recorder that receives every leaf and node, or <see langword="null" />.</param>
    /// <returns>The root; the empty tree's root when there are no leaves.</returns>
    private byte[] Reduce(ReadOnlySpan<byte[]> leafHashes, HashAlgorithm hasher, MerkleTreeDiagnostics? diagnostics)
    {
        var fold = CreateFold(hasher, diagnostics);
        foreach (byte[] leafHash in leafHashes)
            fold.Add(leafHash);

        return fold.Finish();
    }

    /// <summary>
    /// Creates the level fold every root computation reduces with, at this instance's fan-out.
    /// </summary>
    /// <param name="hasher">The algorithm to hash nodes with.</param>
    /// <param name="diagnostics">The recorder that receives every leaf and node, or <see langword="null" />.</param>
    /// <returns>The fold.</returns>
    private MerkleLevelFold CreateFold(HashAlgorithm hasher, MerkleTreeDiagnostics? diagnostics) =>
        new(hasher, HashLength, FanOut, diagnostics);

    /// <summary>
    /// Hashes a domain-separation prefix followed by one or two payload segments.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="prefix">The domain-separation byte.</param>
    /// <param name="first">The first payload segment.</param>
    /// <param name="second">The second payload segment, or empty.</param>
    /// <returns>The resulting hash, <see cref="HashLength" /> bytes long.</returns>
    private byte[] HashWithPrefix(
        HashAlgorithm hasher,
        byte prefix,
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second = default) =>
        MerkleTreeCore.HashWithPrefix(hasher, HashLength, prefix, first, second);

    /// <summary>
    /// Throws when a proof member is called on an instance whose fan-out is not two.
    /// </summary>
    /// <exception cref="NotSupportedException">The instance does not build RFC 6962's binary tree.</exception>
    private void ThrowIfNotBinary()
    {
        if (!IsBinary) throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_MerkleProofsRequireBinaryTree, FanOut));
    }

    /// <summary>
    /// Throws when a degree-of-parallelism argument is neither <c>-1</c> nor a positive count.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The value to validate.</param>
    /// <param name="paramName">The caller-supplied parameter name, captured automatically.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxDegreeOfParallelism" /> is zero or is less than <c>-1</c>.
    /// </exception>
    private static void ThrowIfDegreeOfParallelismInvalid(
        int maxDegreeOfParallelism,
        [CallerArgumentExpression(nameof(maxDegreeOfParallelism))] string? paramName = null)
    {
        if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_OutOfRange_MerkleParallelism,
                    maxDegreeOfParallelism));
        }
    }

    /// <summary>
    /// Rethrows a cancellation raised by an awaited read or a parallel loop as a plain
    /// <see cref="OperationCanceledException" /> carrying the caller's token, so every entry point reports cancellation
    /// the same way.
    /// </summary>
    /// <param name="exception">The cancellation as it was raised.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    /// <returns>The exception to throw.</returns>
    private static OperationCanceledException Normalize(OperationCanceledException exception, CancellationToken cancellationToken) =>
        exception.GetType() == typeof(OperationCanceledException)
            ? exception
            : new OperationCanceledException(exception.Message, exception, cancellationToken);
}
