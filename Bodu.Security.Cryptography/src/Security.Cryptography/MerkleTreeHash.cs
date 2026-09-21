// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a single-threaded Merkle tree hash over any <see cref="HashAlgorithm" />, with a configurable block size
/// and fan-out.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/merkle-tree.svg" alt="Merkle tree construction — the input is sliced into blocks, each block is hashed to a leaf, leaves are grouped by fan-out F and reduced level-by-level until a single root hash remains."/>
/// </para>
/// <para>
/// Input bytes are divided into fixed-size blocks — the top row of the diagram above, with <c>blockSize</c> labeled <b>B</b>.
/// Each block is hashed independently to form a leaf (<em>Level 0</em>). Leaves are then grouped by <c>fanOut</c>
/// (labeled <b>F</b>, shown as 3 in the diagram) and combined into parent nodes, repeating level by level until a
/// single root remains. When a level's final group holds two or more nodes it is hashed like any other; a lone leftover
/// node is <em>promoted</em> to the next level unchanged rather than re-hashed — shown in the diagram as <b>L₇</b>
/// passing through to the next level as itself.
/// </para>
/// <para>
/// <strong>Domain separation and length binding.</strong> Following RFC 6962 §2.1, a leaf is hashed as
/// <c>H(0x00 || block)</c> and an internal node as <c>H(0x01 || child₀ || … || child_{k-1})</c>; the distinct prefix
/// bytes stop an internal node's concatenated child hashes from being replayed as leaf data. The partial tail (the
/// dashed <b>B₇</b> block in the diagram) is hashed at its <em>actual</em> byte length rather than zero-padded, so the
/// exact input length is bound into every leaf and inputs differing only by trailing zeros produce distinct roots. An
/// empty input has no leaves and yields the empty tree's root, <c>H()</c> — the hash of zero bytes.
/// </para>
/// <para>
/// <strong>At the default fan-out of two, this is RFC 6962's tree.</strong> The level-by-level walk with promotion
/// visits exactly the nodes of RFC 6962's recursive definition, so the root is bit-identical to
/// <c>Rfc6962MerkleTree.ComputeRootOfBlocks</c> from the <c>Bodu.Collections</c> package over the same blocks, and that
/// type's inclusion and consistency proofs verify against roots produced here. The two types share this construction in
/// source: the fold and the prefixes are compiled from <c>Bodu.Collections/shared</c>, not duplicated. A wider fan-out
/// is a sound level-by-level commitment of its own — shallower, with wider internal nodes — but RFC 6962 has no k-ary
/// form, so such roots interoperate with nothing outside this package.
/// </para>
/// <para>
/// Each <c>ComputeHash</c> call is a complete computation, so the same instance may be reused across inputs. The
/// <see cref="HashAlgorithm" /> the factory returns is created once and reused for every leaf and node; the one-shot
/// hashing path resets it between nodes. This class is not thread-safe. For parallel leaf hashing over the same tree,
/// see <see cref="ParallelMerkleTreeHash" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Leaf algorithm: any <see cref="HashAlgorithm" />, supplied via an <see cref="IHashAlgorithmFactory{T}" /> or factory
/// delegate.
/// </description>
/// </item>
/// <item>
/// <description>Block size: configurable, default 1024 bytes — the input chunk that becomes one leaf.</description>
/// </item>
/// <item>
/// <description>Fan-out: configurable, default 2 — number of children combined into each parent.</description>
/// </item>
/// <item>
/// <description>
/// Tail handling: the partial final block is hashed at its actual length (length-bound, not zero-padded); a lone
/// leftover node at any level is promoted unchanged.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose MerkleTreeHash.</strong> Pick this when you want a <c>HashAlgorithm</c>-shaped, reusable tree
/// hasher over a leaf algorithm of your choosing — content-addressed storage, chunked integrity over a stream, or a
/// root that must match a transparency-log or other RFC 6962 implementation. For proofs, length-bound roots and the
/// full RFC 6962 surface, use <c>Rfc6962MerkleTree</c>; for maximum throughput over very large inputs use
/// <see cref="ParallelMerkleTreeHash" />; if a fixed tree shape with a fixed leaf hash is acceptable,
/// <see cref="Blake3" /> is faster and ships its own tree mode internally.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // SHA-256 leaves over 4 KiB blocks: RFC 6962's tree at the default fan-out of two.
/// using var merkle = new MerkleTreeHash(
///     algorithmFactory: () => SHA256.Create(),
///     blockSize: 4096);
/// byte[] root = merkle.ComputeHash(payload);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/hashing.html#pattern-6--merkle-trees">Merkle-tree recipes in the hashing guide
/// </seealso> <seealso cref="ParallelMerkleTreeHash"/>
public sealed class MerkleTreeHash
    : IDisposable
{
    /// <summary>The size, in bytes, of each leaf block.</summary>
    private readonly int _blockSize;

    /// <summary>The number of child nodes combined into each parent node during tree reduction.</summary>
    private readonly int _fanOut;

    /// <summary>The factory invoked once to obtain the reused hash algorithm for every leaf and internal node.</summary>
    private readonly Func<HashAlgorithm> _algorithmFactory;

    /// <summary>The lazily-created hash algorithm reused for every leaf and internal node; owned and disposed by this instance.</summary>
    private HashAlgorithm? _hasher;

    /// <summary>The digest length, in bytes, of <see cref="_hasher" />; zero until the algorithm is created.</summary>
    private int _hashLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeHash" /> class with the specified hash algorithm factory,
    /// block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// A typed factory whose <see cref="IHashAlgorithmFactory{T}.Create" /> method is invoked once to obtain the
    /// <see cref="HashAlgorithm" /> instance reused for every leaf and internal node. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="blockSize">The size in bytes of each leaf block. Defaults to 1024.</param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node. Defaults to 2, which is RFC 6962's tree; larger
    /// values produce shallower trees that interoperate with nothing outside this package.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public MerkleTreeHash(IHashAlgorithmFactory<HashAlgorithm> algorithmFactory, int blockSize = 1024, int fanOut = 2)
        : this((algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory))).Create, blockSize, fanOut)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeHash" /> class with the specified hash algorithm factory
    /// delegate, block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// Factory delegate invoked once to obtain the <see cref="HashAlgorithm" /> reused for every leaf and internal
    /// node. Must not be <see langword="null" />.
    /// </param>
    /// <param name="blockSize">The size in bytes of each leaf block. Defaults to 1024.</param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node. Defaults to 2, which is RFC 6962's tree; larger
    /// values produce shallower trees that interoperate with nothing outside this package.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public MerkleTreeHash(Func<HashAlgorithm> algorithmFactory, int blockSize = 1024, int fanOut = 2)
    {
        _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        _blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(
                                                        nameof(blockSize),
                                                        string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));
        _fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), CryptoResourceStrings.Arg_OutOfRange_FanOutMinimum);
    }

    /// <summary>
    /// Computes the Merkle root hash of the data read from <paramref name="input" />.
    /// </summary>
    /// <param name="input">The readable stream to hash to its end. Must not be <see langword="null" />.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <returns>The Merkle root hash of all data read from <paramref name="input" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The stream is read in whole blocks straight into the buffer each leaf is hashed from, and a short read is topped
    /// up rather than taken as the end of the stream, so a network, cryptographic or decompression stream hashes the
    /// same as a memory stream over the same bytes.
    /// </remarks>
    public byte[] ComputeHash(Stream input, MerkleTreeDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        HashAlgorithm hasher = Hasher();
        var fold = new MerkleLevelFold(hasher, _hashLength, _fanOut, diagnostics);
        _ = MerkleTreeCore.ForEachLeafHash(input, _blockSize, hasher, _hashLength, fold.Add, CancellationToken.None);

        return fold.Finish();
    }

    /// <summary>
    /// Computes the Merkle root hash of <paramref name="data" />.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to
    /// record nothing.
    /// </param>
    /// <returns>The Merkle root hash of <paramref name="data" />.</returns>
    public byte[] ComputeHash(ReadOnlySpan<byte> data, MerkleTreeDiagnostics? diagnostics = null)
    {
        HashAlgorithm hasher = Hasher();
        var fold = new MerkleLevelFold(hasher, _hashLength, _fanOut, diagnostics);

        // Every block but the last is full; the last is hashed at its actual length.
        for (int offset = 0; offset < data.Length; offset += _blockSize)
        {
            int length = Math.Min(_blockSize, data.Length - offset);
            fold.Add(MerkleTreeCore.HashWithPrefix(
                hasher,
                _hashLength,
                MerkleTreeFormat.LeafPrefix,
                data.Slice(offset, length)));
        }

        return fold.Finish();
    }

    /// <summary>
    /// Computes the Merkle root hash of a region within <paramref name="data" />.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based index at which to begin reading.</param>
    /// <param name="count">The number of bytes to hash.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to
    /// record nothing.
    /// </param>
    /// <returns>The Merkle root hash of the specified region.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative, or <paramref name="offset" /> +
    /// <paramref name="count" /> exceeds the length of <paramref name="data" />.
    /// </exception>
    public byte[] ComputeHash(byte[] data, int offset, int count, MerkleTreeDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ComputeHash(new ReadOnlySpan<byte>(data, offset, count), diagnostics);
    }

    /// <inheritdoc cref="ComputeHash(ReadOnlySpan{byte}, MerkleTreeDiagnostics?)"/>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    public byte[] ComputeHash(byte[] data, MerkleTreeDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ComputeHash(new ReadOnlySpan<byte>(data), diagnostics);
    }

    /// <summary>
    /// Returns the reused hash algorithm, creating it from the factory on first use and recording its digest length.
    /// </summary>
    /// <returns>The algorithm every leaf and node is hashed with.</returns>
    private HashAlgorithm Hasher()
    {
        if (_hasher is null)
        {
            _hasher = _algorithmFactory();
            _hashLength = _hasher.HashSize >> 3;
        }

        return _hasher;
    }

    /// <inheritdoc />
    public void Dispose() =>
        _hasher?.Dispose();
}
