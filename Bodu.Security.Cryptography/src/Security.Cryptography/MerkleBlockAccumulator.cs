// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Accumulates a byte stream into fixed-size Merkle leaves as it is written, so a root can be produced from the same
/// calls that already feed a flat digest — without a second pass over the input.
/// </summary>
/// <remarks>
/// <para>
/// A writer that streams bytes to storage typically feeds them to an incremental digest as they go. This type gives the
/// Merkle root the same shape: <see cref="Append" /> takes bytes in whatever sizes they arrive, re-blocks them into
/// <see cref="BlockSize" />-byte leaves internally, and folds each completed leaf into the tree at once. The root is
/// therefore independent of how the input was split across calls, and equals the root
/// <c>MerkleTree.ComputeRootOfBlocks</c> computes over the same bytes at the same block size.
/// </para>
/// <para>
/// Memory is one block plus one pending hash per tree level — logarithmic in the leaf count — unless
/// <see cref="RetainsLeafHashes" /> was requested, in which case every leaf hash is kept so
/// <see cref="FinishComputation" /> can hand back a <see cref="MerkleBlockComputation" /> for building authentication
/// paths. A final short block is hashed at its actual length, never padded, and an input that ends exactly on a block
/// boundary produces no empty leaf. An empty input folds to the empty tree's root, <c>H()</c>.
/// </para>
/// <para>
/// <strong>Lifecycle.</strong> Append until the input ends, then call one of the <c>Finish</c> members — any number of
/// times, they return the same result — and <see cref="Reset" /> to start over with the same algorithm and buffer.
/// Appending after a finish throws; a reset clears the finished state. An instance owns one
/// <see cref="HashAlgorithm" /> from the tree's factory and is used from one thread at a time; dispose it when done. A
/// <see cref="MerkleTreeDiagnostics" /> supplied at creation keeps recording across resets, so use a fresh accumulator
/// when a clean trace is wanted.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var tree = new MerkleTree(SHA256.Create);
/// using MerkleBlockAccumulator merkle = tree.CreateBlockAccumulator(blockSize: 1 << 20);
/// using IncrementalHash digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
///
/// while (TryReadChunk(out ReadOnlySpan<byte> chunk))
/// {
///     digest.AppendData(chunk);   // the flat digest the index already carries
///     merkle.Append(chunk);       // the Merkle root, from the same bytes, in the same pass
/// }
///
/// byte[] flatDigest = digest.GetHashAndReset();
/// byte[] boundRoot = merkle.FinishBound();   // H(0x02 || u64_be(length) || MTH)
///]]>
/// </code>
/// </example>
/// <seealso cref="MerkleTree.CreateBlockAccumulator(int, bool, MerkleTreeDiagnostics)" />
public sealed class MerkleBlockAccumulator
    : IDisposable
{
    /// <summary>The tree whose construction and hash the accumulator follows.</summary>
    private readonly MerkleTree _tree;

    /// <summary>The algorithm every leaf and node is hashed with, owned for the accumulator's lifetime.</summary>
    private readonly HashAlgorithm _hasher;

    /// <summary>Receives every leaf and hashed node, or <see langword="null" /> when nothing is recorded.</summary>
    private readonly MerkleTreeDiagnostics? _diagnostics;

    /// <summary>The leaf prefix at index zero followed by the bytes of the block being filled.</summary>
    private readonly byte[] _buffer;

    /// <summary>Whether every leaf hash is kept for <see cref="FinishComputation" />.</summary>
    private readonly bool _retainLeafHashes;

    /// <summary>The fold that reduces completed leaves; replaced by <see cref="Reset" />.</summary>
    private MerkleTree.LevelFold _fold;

    /// <summary>The retained leaf hashes, or <see langword="null" /> when they are not kept.</summary>
    private List<byte[]>? _leafHashes;

    /// <summary>The number of payload bytes in <see cref="_buffer" /> not yet hashed.</summary>
    private int _filled;

    /// <summary>The number of bytes appended since construction or the last reset.</summary>
    private long _length;

    /// <summary>The root once finished, or <see langword="null" /> while still accepting input.</summary>
    private byte[]? _root;

    /// <summary>Whether <see cref="Dispose" /> has run.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleBlockAccumulator" /> class.
    /// </summary>
    /// <param name="tree">The tree whose construction and hash the accumulator follows.</param>
    /// <param name="hasher">A fresh algorithm from the tree's factory, owned by the accumulator.</param>
    /// <param name="blockSize">The size, in bytes, of each leaf block.</param>
    /// <param name="retainLeafHashes">Whether to keep every leaf hash for <see cref="FinishComputation" />.</param>
    /// <param name="diagnostics">The recorder that receives every node, or <see langword="null" />.</param>
    internal MerkleBlockAccumulator(
        MerkleTree tree,
        HashAlgorithm hasher,
        int blockSize,
        bool retainLeafHashes,
        MerkleTreeDiagnostics? diagnostics)
    {
        _tree = tree;
        _hasher = hasher;
        _diagnostics = diagnostics;
        _retainLeafHashes = retainLeafHashes;
        BlockSize = blockSize;

        _buffer = new byte[blockSize + 1];
        _buffer[0] = MerkleTree.LeafPrefix;
        _fold = CreateFold();
        _leafHashes = retainLeafHashes ? [] : null;
    }

    /// <summary>
    /// Gets the size, in bytes, of each leaf block.
    /// </summary>
    public int BlockSize { get; }

    /// <summary>
    /// Gets the length, in bytes, of every leaf hash and of the root.
    /// </summary>
    public int HashLength => _tree.HashLength;

    /// <summary>
    /// Gets the number of bytes appended since construction or the last <see cref="Reset" />.
    /// </summary>
    public long Length => _length;

    /// <summary>
    /// Gets the number of leaves hashed so far.
    /// </summary>
    /// <value>
    /// The number of completed blocks; a partially filled final block is counted only once a <c>Finish</c> member has
    /// hashed it.
    /// </value>
    public long LeafCount => _fold.LeafCount;

    /// <summary>
    /// Gets a value indicating whether every leaf hash is being kept, so <see cref="FinishComputation" /> is available.
    /// </summary>
    public bool RetainsLeafHashes => _retainLeafHashes;

    /// <summary>
    /// Gets a value indicating whether a <c>Finish</c> member has been called since construction or the last
    /// <see cref="Reset" />, after which <see cref="Append" /> is refused.
    /// </summary>
    public bool IsFinished => _root is not null;

    /// <summary>
    /// Appends bytes to the input, hashing every leaf block they complete.
    /// </summary>
    /// <param name="source">The next bytes of the input, in any size.</param>
    /// <exception cref="ObjectDisposedException">The accumulator has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// A <c>Finish</c> member has already been called; call <see cref="Reset" /> to start a new computation.
    /// </exception>
    /// <remarks>
    /// Bytes are copied into the block being filled and hashed only when it is full, so a partial block is never
    /// emitted mid-stream and the leaves are the same however the input was chunked. An empty span is accepted and
    /// changes nothing.
    /// </remarks>
    public void Append(ReadOnlySpan<byte> source)
    {
        ThrowHelper.ThrowIfDisposed(_disposed, nameof(MerkleBlockAccumulator));
        ThrowIfFinished();

        while (!source.IsEmpty)
        {
            int take = Math.Min(BlockSize - _filled, source.Length);
            source[..take].CopyTo(_buffer.AsSpan(1 + _filled));
            _filled += take;
            _length += take;
            source = source[take..];

            if (_filled == BlockSize)
                EmitLeaf();
        }
    }

    /// <summary>
    /// Hashes any partial final block and returns the tree's root.
    /// </summary>
    /// <returns>The root; the empty tree's root, <c>H()</c>, when nothing was appended.</returns>
    /// <exception cref="ObjectDisposedException">The accumulator has been disposed.</exception>
    /// <remarks>
    /// The first call completes the computation; later calls return the same root without further work. The root is
    /// identical to <c>MerkleTree.ComputeRootOfBlocks</c> over the same bytes at <see cref="BlockSize" />.
    /// </remarks>
    public byte[] Finish()
    {
        ThrowHelper.ThrowIfDisposed(_disposed, nameof(MerkleBlockAccumulator));

        if (_root is null)
        {
            if (_filled > 0)
                EmitLeaf();

            _root = _fold.Finish();
        }

        return (byte[])_root.Clone();
    }

    /// <summary>
    /// Finishes the computation and returns the root bound to the input's byte length,
    /// <c>H(0x02 || u64_be(length) || root)</c>.
    /// </summary>
    /// <returns>The length-bound root.</returns>
    /// <exception cref="ObjectDisposedException">The accumulator has been disposed.</exception>
    /// <remarks>
    /// This is the commitment to publish when a verifier will later be told the input's length by a party it does not
    /// trust: <see cref="MerkleTree.VerifyBlockInclusion" /> derives the tree size from the bound length, so a
    /// misstated length fails closed. It is exactly <see cref="MerkleTree.BindRoot" /> applied to <see cref="Finish" />
    /// and <see cref="Length" />.
    /// </remarks>
    public byte[] FinishBound() =>
        _tree.BindRoot(Finish(), _length);

    /// <summary>
    /// Finishes the computation and returns it together with the retained leaf hashes, so authentication paths can be
    /// built without a second pass over the input.
    /// </summary>
    /// <returns>The root, input length, block size and ordered leaf hashes.</returns>
    /// <exception cref="ObjectDisposedException">The accumulator has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// The accumulator was created without <c>retainLeafHashes</c>, so no leaf hashes were kept.
    /// </exception>
    /// <remarks>
    /// The result is what <c>MerkleTree.ComputeBlocked</c> would have returned over the same bytes. A subsequent
    /// <see cref="Reset" /> does not disturb it: the returned computation keeps its own list.
    /// </remarks>
    public MerkleBlockComputation FinishComputation()
    {
        ThrowHelper.ThrowIfDisposed(_disposed, nameof(MerkleBlockAccumulator));
        if (_leafHashes is null) throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_MerkleLeafHashesNotRetained);

        byte[] root = Finish();
        return new MerkleBlockComputation(root, _length, BlockSize, _leafHashes);
    }

    /// <summary>
    /// Discards the current input and any finished root so the accumulator can take a new input.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The accumulator has been disposed.</exception>
    /// <remarks>
    /// The hash algorithm and the block buffer are reused; the fold and, when leaf hashes are retained, the list are
    /// replaced, so a <see cref="MerkleBlockComputation" /> already handed out is unaffected.
    /// </remarks>
    public void Reset()
    {
        ThrowHelper.ThrowIfDisposed(_disposed, nameof(MerkleBlockAccumulator));

        Array.Clear(_buffer, 1, BlockSize);
        _filled = 0;
        _length = 0;
        _root = null;
        _fold = CreateFold();
        _leafHashes = _retainLeafHashes ? [] : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Disposes the owned <see cref="HashAlgorithm" /> and clears the block buffer. Every other member then throws
    /// <see cref="ObjectDisposedException" />; disposing again does nothing.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _hasher.Dispose();
        Array.Clear(_buffer);
    }

    /// <summary>
    /// Hashes the block being filled as the next leaf and folds it into the tree.
    /// </summary>
    private void EmitLeaf()
    {
        byte[] leaf = MerkleTree.HashBuffer(_hasher, HashLength, _buffer.AsSpan(0, 1 + _filled));
        _filled = 0;

        _leafHashes?.Add(leaf);
        _fold.Add(leaf);
    }

    /// <summary>
    /// Creates a fresh fold over the owned algorithm and the optional recorder, at the tree's fan-out.
    /// </summary>
    /// <returns>The fold.</returns>
    private MerkleTree.LevelFold CreateFold() =>
        new(_hasher, HashLength, _tree.FanOut, _diagnostics);

    /// <summary>
    /// Throws when a <c>Finish</c> member has already completed the computation.
    /// </summary>
    /// <exception cref="InvalidOperationException">The computation is finished.</exception>
    private void ThrowIfFinished()
    {
        if (_root is not null) throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_MerkleAccumulatorFinished);
    }
}
