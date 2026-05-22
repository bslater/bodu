// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;
using Bodu.Buffers;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a single-threaded Merkle tree hash implementation with configurable hash algorithm, block size, and
/// fan-out.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/merkle-tree.svg" alt="Merkle tree construction — the input is sliced into blocks, each block is hashed to a leaf, leaves are grouped by fan-out F and reduced level-by-level until a single root hash remains; a partial tail block is zero-padded to the full block size before hashing."/>
/// </para>
/// <para>
/// Input bytes are divided into fixed-size blocks — the top row of the diagram above, with <c>blockSize</c> labeled <b>
/// B</b>. Each block is hashed independently to form a leaf node (<em>Level 0</em>). Leaf hashes are then grouped by
/// <c>fanOut</c> (labeled <b>F</b>, shown as 3 in the diagram) and combined into parent nodes, repeating level by level
/// until a single root hash remains at the top.
/// </para>
/// <para>
/// When the input length is not a multiple of <c>blockSize</c>, the partial tail (the dashed orange <b>B₇</b> block in
/// the diagram) is zero-padded up to a full block before hashing, so every leaf is the same width regardless of input
/// alignment. If the final group at any internal level contains fewer than <c>fanOut</c> children, that short group is
/// promoted with its surviving children only — shown in the diagram as the single-edged reduction of <b>L₇</b> into <b>
/// N₃</b>.
/// </para>
/// <para>
/// Each call to a <c>ComputeHash</c> overload resets internal state, so the same instance may be reused across multiple
/// inputs without re-construction.
/// </para>
/// <para>
/// This class is not thread-safe. Concurrent calls from multiple threads produce undefined results. For a concurrent
/// level-worker pipeline over the same tree structure, see <see cref="ParallelMerkleTreeHash" />.
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
/// <description>
/// Block size: configurable, default 1024 bytes — the input chunk that becomes one leaf.
/// </description>
/// </item>
/// <item>
/// <description>
/// Fan-out: configurable, default 3 — number of children combined into each parent.
/// </description>
/// </item>
/// <item>
/// <description>
/// Tail handling: partial final blocks are zero-padded; short final groups at internal levels promote without padding.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose MerkleTreeHash.</strong> Pick this when you need explicit control over the leaf hash, block
/// size, or fan-out — content-addressed storage with audit-friendly tree shapes, BitTorrent-style chunked integrity, or
/// producing root hashes whose construction must match a specific protocol. For maximum throughput over very large
/// inputs use <see cref="ParallelMerkleTreeHash" />; if a fixed tree shape with a fixed leaf hash is acceptable,
/// <see cref="Blake3" /> is faster and ships its own tree mode internally.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // SHA-256 leaves, 4 KiB blocks, fan-out of 4.
/// using var merkle = new MerkleTreeHash(
///     algorithmFactory: () => SHA256.Create(),
///     blockSize: 4096,
///     fanOut: 4);
/// byte[] root = merkle.ComputeHash(payload);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/hashing.html#pattern-6--merkle-trees">Merkle-tree recipes in the hashing guide
/// </seealso> <seealso cref="ParallelMerkleTreeHash"/>
public sealed class MerkleTreeHash
    : IDisposable
{
    private readonly int _blockSize;
    private readonly int _fanOut;
    private readonly Func<HashAlgorithm> _algorithmFactory;

    // Accumulates raw bytes for the current partial block; reused across ComputeHash calls.
    private readonly MemoryStream _buffer;

    // Holds the hash values produced at the current tree level during reduction.
    private List<byte[]> _currentLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeHash" /> class with the specified hash algorithm factory,
    /// block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// A typed factory whose <see cref="IHashAlgorithmFactory{T}.Create" /> method is invoked once per hash operation
    /// to obtain a fresh, independent <see cref="HashAlgorithm" /> instance. Must not be <see langword="null" />. A
    /// distinct instance is created for each leaf and internal node so that no algorithm state is shared across
    /// operations.
    /// </param>
    /// <param name="blockSize">
    /// The size in bytes of each leaf block. Must be greater than zero. Defaults to 1024.
    /// </param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node during tree reduction. Must be at least 2. Defaults to
    /// 3. Larger values produce shallower trees.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public MerkleTreeHash(IHashAlgorithmFactory<HashAlgorithm> algorithmFactory, int blockSize = 1024, int fanOut = 3)
        : this((algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory))).Create, blockSize, fanOut)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleTreeHash" /> class with the specified hash algorithm factory
    /// delegate, block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// Factory delegate that returns a fresh <see cref="HashAlgorithm" /> per hash operation. Must not be
    /// <see langword="null" />. A distinct instance is created for each leaf and internal node so that no algorithm
    /// state is shared across operations.
    /// </param>
    /// <param name="blockSize">
    /// The size in bytes of each leaf block. Must be greater than zero. Defaults to 1024.
    /// </param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node during tree reduction. Must be at least 2. Defaults to
    /// 3. Larger values produce shallower trees.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public MerkleTreeHash(Func<HashAlgorithm> algorithmFactory, int blockSize = 1024, int fanOut = 3)
    {
        this._algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        this._blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(
                                                        nameof(blockSize),
                                                        string.Format(CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));
        this._fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), CryptoResourceStrings.Arg_OutOfRange_FanOutMinimum);
        this._buffer = new MemoryStream(blockSize);
        this._currentLevel = new List<byte[]>();
    }

    /// <summary>
    /// Computes the Merkle root hash of the data read from <paramref name="input" />.
    /// </summary>
    /// <param name="input">The readable stream to hash. Must not be <see langword="null" />.</param>
    /// <returns>A byte array containing the Merkle root hash of all data read from <paramref name="input" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    public byte[] ComputeHash(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        this.Reset();

        var buffer = ArrayPool<byte>.Shared.Rent(this._blockSize * 4);
        try
        {
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                this.ProcessInput(buffer.AsSpan(0, bytesRead));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        return this.ComputeFinalHash();
    }

    /// <summary>
    /// Computes the Merkle root hash of <paramref name="data" />.
    /// </summary>
    /// <param name="data">The byte span to hash.</param>
    /// <returns>A byte array containing the Merkle root hash of <paramref name="data" />.</returns>
    public byte[] ComputeHash(ReadOnlySpan<byte> data)
    {
        this.Reset();
        this.ProcessInput(data);
        return this.ComputeFinalHash();
    }

    /// <summary>
    /// Computes the Merkle root hash of a region within <paramref name="data" />.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based index at which to begin reading.</param>
    /// <param name="count">The number of bytes to hash.</param>
    /// <returns>A byte array containing the Merkle root hash of the specified region.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative, or <paramref name="offset" /> +
    /// <paramref name="count" /> exceeds the length of <paramref name="data" />.
    /// </exception>
    public byte[] ComputeHash(byte[] data, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        return this.ComputeHash(new ReadOnlySpan<byte>(data, offset, count));
    }

    /// <inheritdoc cref="ComputeHash(ReadOnlySpan{byte})"/>
    public byte[] ComputeHash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return this.ComputeHash(new ReadOnlySpan<byte>(data));
    }

    // -----------------------------------------------------------------------------------------
    // Internal pipeline
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Clears all accumulated leaf buffers and intermediate state so the instance is ready to compute a new hash.
    /// </summary>
    private void Reset()
    {
        this._buffer.SetLength(0);
        this._currentLevel.Clear();
    }

    /// <summary>
    /// Appends <paramref name="data" /> to the current leaf accumulator, flushing full leaf-sized blocks into the tree
    /// as they become available.
    /// </summary>
    /// <param name="data">The input bytes to feed into the hash.</param>
    private void ProcessInput(ReadOnlySpan<byte> data)
    {
        while (!data.IsEmpty)
        {
            var toWrite = Math.Min(this._blockSize - (int)this._buffer.Length, data.Length);
            this._buffer.Write(data[..toWrite]);
            data = data[toWrite..];

            if (this._buffer.Length == this._blockSize)
            {
                this._currentLevel.Add(this.ComputeLeafHash(this._buffer));
                this._buffer.SetLength(0);
            }
        }
    }

    /// <summary>
    /// Finalizes any trailing partial leaf, combines intermediate node hashes up the tree, and returns the root hash.
    /// </summary>
    /// <returns>The root Merkle-tree hash bytes.</returns>
    private byte[] ComputeFinalHash()
    {
        // Zero-pad the partial tail block to a full block size before hashing, so that every
        // leaf is the same width regardless of input alignment. MemoryStream.SetLength fills
        // the extended region with zeros when growing.
        if (this._buffer.Length > 0)
        {
            this._buffer.SetLength(this._blockSize);
            this._currentLevel.Add(this.ComputeLeafHash(this._buffer));
            this._buffer.SetLength(0);
        }

        // Reduce level by level until a single root hash remains.
        while (this._currentLevel.Count > 1)
        {
            var hashLength = this._currentLevel[0].Length;
            var nextLevel = new List<byte[]>((this._currentLevel.Count / this._fanOut) + 1);

            for (var i = 0; i < this._currentLevel.Count; i += this._fanOut)
            {
                var groupSize = Math.Min(this._fanOut, this._currentLevel.Count - i);

                using var bufferBuilder = new PooledBufferBuilder<byte>(hashLength * groupSize);
                for (var j = 0; j < groupSize; j++)
                    bufferBuilder.AppendRange(this._currentLevel[i + j].AsSpan());

                nextLevel.Add(this.ComputeLeafHash(bufferBuilder.WrittenSpan));
            }

            this._currentLevel = nextLevel;
        }

        return this._currentLevel[0];
    }

    // -----------------------------------------------------------------------------------------
    // Hashing helpers
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Computes the leaf hash over the full contents of <paramref name="stream" /> using the configured
    /// factory-produced <see cref="HashAlgorithm" />.
    /// </summary>
    /// <param name="stream">The memory stream containing the leaf bytes.</param>
    /// <returns>The leaf hash bytes.</returns>
    private byte[] ComputeLeafHash(MemoryStream stream)
    {
        stream.Position = 0;
        using HashAlgorithm hasher = this._algorithmFactory();
        return hasher.ComputeHash(stream);
    }

    /// <summary>
    /// Computes the leaf hash over <paramref name="span" /> using the configured factory-produced
    /// <see cref="HashAlgorithm" />.
    /// </summary>
    /// <param name="span">The leaf bytes.</param>
    /// <returns>The leaf hash bytes.</returns>
    private byte[] ComputeLeafHash(ReadOnlySpan<byte> span)
    {
        using HashAlgorithm hasher = this._algorithmFactory();
        var result = new byte[hasher.HashSize >> 3];
        CryptoHelpers.ThrowIfHashAlgorithmDestinationTooSmall(
            hasher.TryComputeHash(span, result, out var bytesWritten));
        if (bytesWritten == result.Length)
            return result;

        var trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(result, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    // -----------------------------------------------------------------------------------------
    // Disposal
    // -----------------------------------------------------------------------------------------

    /// <inheritdoc />
    public void Dispose() => this._buffer.Dispose();
}
