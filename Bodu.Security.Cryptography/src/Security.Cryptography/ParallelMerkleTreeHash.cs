// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a Merkle tree hash that hashes leaves in parallel, over any <see cref="HashAlgorithm" />, with a
/// configurable block size and fan-out.
/// </summary>
/// <remarks>
/// <para>
/// The input is divided into fixed-size blocks and read in batches; each batch's blocks are hashed to leaves
/// concurrently, one <see cref="HashAlgorithm" /> per worker, and the leaves are then folded in order into the same
/// level-by-level tree <see cref="MerkleTreeHash" /> builds. Leaf hashing is where the time goes — every block's bytes
/// pass through the algorithm once — while the reduction hashes one digest-sized node per group and is a negligible
/// fraction of the work at any realistic block size, so parallelizing the leaves is what makes the difference and the
/// fold stays sequential and simple.
/// </para>
/// <para>
/// <strong>Same tree, same roots.</strong> For a given algorithm, block size and fan-out this type produces exactly the
/// root <see cref="MerkleTreeHash" /> produces; the two are facades over one shared fold. At the default fan-out of two
/// that is RFC 6962's Merkle Tree Hash, bit-identical to <c>MerkleTree.ComputeRootParallel</c> from the
/// <c>Bodu.Collections</c> package, whose inclusion and consistency proofs verify against roots produced here. A wider
/// fan-out is a sound commitment of its own but is not RFC 6962's, which is binary by definition. A short tail block is
/// hashed at its actual length, never zero-padded, and an empty input yields the empty tree's root, <c>H()</c>.
/// </para>
/// <para>
/// <b>Reuse:</b> the same instance may be used for multiple sequential hash computations; nothing is retained between
/// calls. Concurrent calls on one instance are not supported. Every <see cref="HashAlgorithm" /> a computation creates
/// is disposed before the call returns, on success, fault and cancellation alike, and <see cref="Dispose" /> cancels a
/// computation still in flight.
/// </para>
/// <para>
/// <b>When to choose ParallelMerkleTreeHash.</b> Large inputs where leaf hashing dominates — multi-gigabyte files or
/// streams — with a leaf algorithm of your choosing. For proofs and the full RFC 6962 surface, use
/// <c>MerkleTree</c>; for small inputs <see cref="MerkleTreeHash" /> avoids the parallel scheduling overhead.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // SHA-256 leaves over 64 KiB blocks — RFC 6962's tree at the default fan-out of two.
/// using var merkle = new ParallelMerkleTreeHash(
///     algorithmFactory: () => SHA256.Create(),
///     blockSize: 64 * 1024);
///
/// await using FileStream file = File.OpenRead(path);
/// byte[] root = await merkle.ComputeHashAsync(file);
///]]>
/// </code>
/// </example>
/// <seealso cref="MerkleTreeHash" />
public sealed class ParallelMerkleTreeHash
    : IDisposable
{
    /// <summary>The size, in bytes, of each leaf block.</summary>
    private readonly int _blockSize;

    /// <summary>The number of child nodes combined into each parent node during tree reduction.</summary>
    private readonly int _fanOut;

    /// <summary>The factory that supplies one fresh algorithm per parallel worker and one for the reduction.</summary>
    private readonly Func<HashAlgorithm> _algorithmFactory;

    /// <summary>Cancelled by <see cref="Dispose" /> so a computation still in flight stops.</summary>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Whether <see cref="Dispose" /> has run.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelMerkleTreeHash" /> class with the specified hash algorithm
    /// factory, block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// A typed factory whose <see cref="IHashAlgorithmFactory{T}.Create" /> method returns a fresh, independent
    /// <see cref="HashAlgorithm" /> on each call. Must not be <see langword="null" />. One instance is created per
    /// parallel worker and one for the reduction, so no algorithm state is ever shared between threads.
    /// </param>
    /// <param name="blockSize">The size in bytes of each leaf block. Defaults to 4096.</param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node. Defaults to 2, which is RFC 6962's tree; larger values
    /// produce shallower trees that interoperate with nothing outside this package.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public ParallelMerkleTreeHash(
        IHashAlgorithmFactory<HashAlgorithm> algorithmFactory,
        int blockSize = 4096,
        int fanOut = 2)
        : this((algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory))).Create, blockSize, fanOut)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelMerkleTreeHash" /> class with the specified hash algorithm
    /// factory delegate, block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    /// Factory delegate that returns a fresh, independent <see cref="HashAlgorithm" /> on each call. Must not be
    /// <see langword="null" />. One instance is created per parallel worker and one for the reduction, so no algorithm
    /// state is ever shared between threads.
    /// </param>
    /// <param name="blockSize">The size in bytes of each leaf block. Defaults to 4096.</param>
    /// <param name="fanOut">
    /// The number of child nodes combined into each parent node. Defaults to 2, which is RFC 6962's tree; larger values
    /// produce shallower trees that interoperate with nothing outside this package.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="fanOut" /> is less than 2.
    /// </exception>
    public ParallelMerkleTreeHash(
        Func<HashAlgorithm> algorithmFactory,
        int blockSize = 4096,
        int fanOut = 2)
    {
        _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        _blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(
                                                        nameof(blockSize),
                                                        string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));
        _fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), CryptoResourceStrings.Arg_OutOfRange_FanOutMinimum);
    }

    /// <summary>
    /// Asynchronously reads <paramref name="input" /> in batches of fixed-size blocks, hashes each batch's leaves in
    /// parallel, and returns the Merkle root once all data has been processed.
    /// </summary>
    /// <param name="input">The readable stream to hash to its end. Must not be <see langword="null" />.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed between reads and between blocks.</param>
    /// <returns>A task whose result is the Merkle root hash.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="TaskCanceledException">
    /// <paramref name="cancellationToken" /> was cancelled, or the instance was disposed during the computation.
    /// </exception>
    /// <remarks>
    /// A short read is topped up rather than taken as the end of the stream, so a network, cryptographic or
    /// decompression stream hashes the same as a memory stream over the same bytes; only a read returning zero ends the
    /// input. Cancellation, whether observed before the first read, by the stream, or by the parallel leaf hashing,
    /// always surfaces as <see cref="TaskCanceledException" />.
    /// </remarks>
    public async Task<byte[]> ComputeHashAsync(
        Stream input,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ThrowIfDisposed();

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        CancellationToken token = linked.Token;

        using HashAlgorithm reducer = _algorithmFactory();
        int hashLength = reducer.HashSize >> 3;
        var fold = new MerkleLevelFold(reducer, hashLength, _fanOut, diagnostics);

        int batchSize = MerkleTreeCore.ResolveBatchSize(-1);
        byte[]?[] batch = new byte[]?[batchSize];
        int[] batchLengths = new int[batchSize];
        byte[][] batchHashes = new byte[batchSize][];
        try
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException(null, null, token);

                // Fill the batch sequentially: the leaf prefix sits at index 0 so each block is hashed straight out
                // of the buffer it was read into.
                int filledBlocks = 0;
                while (filledBlocks < batchSize)
                {
                    batch[filledBlocks] ??= ArrayPool<byte>.Shared.Rent(_blockSize + 1);
                    byte[] buffer = batch[filledBlocks]!;
                    buffer[0] = MerkleTreeFormat.LeafPrefix;

                    int filled = 0;
                    while (filled < _blockSize)
                    {
                        int read = await input
                            .ReadAsync(buffer.AsMemory(1 + filled, _blockSize - filled), token)
                            .ConfigureAwait(false);
                        if (read <= 0)
                            break;

                        filled += read;
                    }

                    if (filled == 0)
                        break;

                    batchLengths[filledBlocks] = filled;
                    filledBlocks++;

                    if (filled < _blockSize)
                        break;
                }

                if (filledBlocks == 0)
                    break;

                HashBatchInParallel(batch, batchLengths, batchHashes, filledBlocks, hashLength, token);

                for (int index = 0; index < filledBlocks; index++)
                    fold.Add(batchHashes[index]);

                // A partially-filled final block ends the stream.
                if (batchLengths[filledBlocks - 1] < _blockSize)
                    break;
            }
        }
        catch (OperationCanceledException ex) when (ex is not TaskCanceledException)
        {
            // The parallel loop reports cancellation as the base type; the async surface promises one type.
            throw new TaskCanceledException(ex.Message, ex, token);
        }
        finally
        {
            foreach (byte[]? buffer in batch)
            {
                if (buffer is not null)
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        return fold.Finish();
    }

    /// <summary>
    /// Computes the Merkle root hash of <paramref name="data" />, hashing its blocks in parallel.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <returns>The Merkle root hash of <paramref name="data" />.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The instance was disposed during the computation.</exception>
    public byte[] ComputeHash(ReadOnlyMemory<byte> data, MerkleTreeDiagnostics? diagnostics = null)
    {
        ThrowIfDisposed();

        using HashAlgorithm reducer = _algorithmFactory();
        int hashLength = reducer.HashSize >> 3;
        var fold = new MerkleLevelFold(reducer, hashLength, _fanOut, diagnostics);

        long blockCount = MerkleTreeCore.BlockCount(data.Length, _blockSize);
        if (blockCount == 0)
            return fold.Finish();

        byte[][] leafHashes = new byte[blockCount][];
        int blockSize = _blockSize;
        var options = new ParallelOptions { CancellationToken = _cts.Token };

        RunParallel(() => Parallel.For(
            0,
            (int)blockCount,
            options,
            _algorithmFactory,
            (index, _, hasher) =>
            {
                int offset = index * blockSize;
                int length = Math.Min(blockSize, data.Length - offset);
                leafHashes[index] = MerkleTreeCore.HashWithPrefix(
                    hasher,
                    hashLength,
                    MerkleTreeFormat.LeafPrefix,
                    data.Span.Slice(offset, length));
                return hasher;
            },
            static hasher => hasher.Dispose()));

        foreach (byte[] leafHash in leafHashes)
            fold.Add(leafHash);

        return fold.Finish();
    }

    /// <summary>
    /// Computes the Merkle root hash of <paramref name="data" />, hashing its blocks in parallel.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <returns>The Merkle root hash of <paramref name="data" />.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The instance was disposed during the computation.</exception>
    /// <remarks>
    /// A span cannot be captured by the parallel workers, so its bytes are copied once into a pooled buffer for the
    /// duration of the call. Prefer the <see cref="ReadOnlyMemory{T}" /> or array overloads when the input is already
    /// in a buffer.
    /// </remarks>
    public byte[] ComputeHash(ReadOnlySpan<byte> data, MerkleTreeDiagnostics? diagnostics = null)
    {
        ThrowIfDisposed();

        byte[] rented = ArrayPool<byte>.Shared.Rent(Math.Max(data.Length, 1));
        try
        {
            data.CopyTo(rented);
            return ComputeHash(rented.AsMemory(0, data.Length), diagnostics);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    /// <summary>
    /// Computes the Merkle root hash of <paramref name="data" />, hashing its blocks in parallel.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null" />.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <returns>The Merkle root hash of <paramref name="data" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public byte[] ComputeHash(byte[] data, MerkleTreeDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ComputeHash(data.AsMemory(), diagnostics);
    }

    /// <summary>
    /// Computes the Merkle root hash of a region within <paramref name="data" />, hashing its blocks in parallel.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based index at which to begin reading.</param>
    /// <param name="count">The number of bytes to hash.</param>
    /// <param name="diagnostics">
    /// A recorder that receives every leaf and internal node as the tree is built, or <see langword="null" /> to record
    /// nothing.
    /// </param>
    /// <returns>The Merkle root hash of the specified region.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative, or <paramref name="offset" /> +
    /// <paramref name="count" /> exceeds the length of <paramref name="data" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public byte[] ComputeHash(byte[] data, int offset, int count, MerkleTreeDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ComputeHash(new ReadOnlyMemory<byte>(data, offset, count), diagnostics);
    }

    /// <summary>
    /// Hashes the filled blocks of a batch to leaves in parallel, one algorithm instance per worker.
    /// </summary>
    /// <param name="batch">The block buffers, each carrying the leaf prefix at index zero.</param>
    /// <param name="batchLengths">The number of payload bytes in each buffer.</param>
    /// <param name="batchHashes">Receives each block's leaf hash at the block's index.</param>
    /// <param name="filledBlocks">The number of buffers holding a block.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="cancellationToken">A token the parallel loop observes.</param>
    private void HashBatchInParallel(
        byte[]?[] batch,
        int[] batchLengths,
        byte[][] batchHashes,
        int filledBlocks,
        int hashLength,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { CancellationToken = cancellationToken };

        RunParallel(() => Parallel.For(
            0,
            filledBlocks,
            options,
            _algorithmFactory,
            (index, _, hasher) =>
            {
                batchHashes[index] = MerkleTreeCore.HashBuffer(
                    hasher,
                    hashLength,
                    batch[index].AsSpan(0, 1 + batchLengths[index]));
                return hasher;
            },
            static hasher => hasher.Dispose()));
    }

    /// <summary>
    /// Runs a parallel loop and surfaces a worker fault as itself rather than wrapped in an
    /// <see cref="AggregateException" />, so a faulting leaf algorithm is reported the way the sequential type reports
    /// it.
    /// </summary>
    /// <param name="loop">The parallel loop to run.</param>
    /// <remarks>
    /// Cancellation surfaces as the <see cref="OperationCanceledException" /> the loop itself throws. Every worker's
    /// algorithm is disposed by the loop's local-finalizer whether the loop completes, faults or is cancelled.
    /// </remarks>
    private static void RunParallel(Action loop)
    {
        try
        {
            loop();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
        {
            // Several workers can fault on the same input at once; the first fault is the one a sequential
            // computation would have raised, and the others are its echoes.
            ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <inheritdoc />
    /// <remarks>
    /// Cancels a computation still in flight; a call in progress on another thread surfaces
    /// <see cref="OperationCanceledException" />.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
