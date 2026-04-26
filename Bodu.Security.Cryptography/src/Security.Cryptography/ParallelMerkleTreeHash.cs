// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a parallel Merkle tree hash implementation using a concurrent level-worker pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/parallel-merkle-tree.svg" alt="Swim-lane diagram showing the Dispatcher thread reading input chunks, slicing them into blocks, hashing each block into a leaf, and submitting leaves into ch L₀; one worker task per tree level consumes from its own channel, groups F incoming nodes, hashes them, and submits the parent to the next level's channel. Adjacent lanes are shown active at the same timestep to emphasise parallelism." />
/// </para>
/// <para>
/// The swim-lane diagram above traces a single <c>ComputeHashAsync</c> call across wall-clock time.
/// Each horizontal band is a <em>distinct .NET thread or async Task</em>, and each vertical column
/// (<c>t₁…t₄</c>) is a point in time:
/// <list type="number">
/// <item><description>
///   <b>Dispatcher lane (producer).</b> The caller's thread inside <c>ComputeHashAsync</c> reads the
///   input stream in 8×<c>blockSize</c> chunks, slices each chunk into <c>blockSize</c>-wide blocks,
///   invokes <c>_algorithmFactory()</c> to compute one leaf hash per block, and writes each leaf into
///   <b>ch L₀</b> — the purple arrows leaving the amber boxes at the top of the diagram.
/// </description></item>
/// <item><description>
///   <b>Level-worker lanes (consumers).</b> One <see cref="Channel{T}" /> and one background
///   <see cref="Task" /> are created lazily per tree level as the input grows. Each worker awaits on
///   its channel, accumulates nodes until it has <c>fanOut</c> of them, concatenates and hashes that
///   group, and enqueues the parent into the next level's channel — the blue and teal boxes, with
///   purple channel arrows crossing the lane boundaries.
/// </description></item>
/// <item><description>
///   <b>Parallelism.</b> The key insight is the <em>column</em> at <c>t₃</c>: the Dispatcher is still
///   hashing leaves while the Level 0 worker is hashing its second pair <em>and</em> the Level 1 worker
///   has already produced its first internal node. Three lanes are active simultaneously — indicated
///   by the "three lanes active" brace beneath the time axis. Tree reduction therefore <em>overlaps</em>
///   with continued leaf production rather than running after it.
/// </description></item>
/// <item><description>
///   <b>Root extraction.</b> The bottom lane activates only at the end: the last surviving node on
///   <c>ch L₂</c> is recognised as the root and returned to the caller.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// A short tail block is zero-padded to a full <c>blockSize</c> before hashing, so every leaf is the
/// same width regardless of input alignment — the Dispatcher's <c>L₆</c> event in the diagram above
/// is the tail case. A short final group at any internal level is promoted with its surviving children
/// only; the Level 1 <c>hash(M₁, N₃)</c> event at <c>t₄</c> is exactly this case (two children, one of
/// which was itself promoted from a short group).
/// </para>
/// <para>
/// <b>Reuse:</b> the same instance may be used for multiple sequential hash computations.
/// At the start of each call, all per-computation state is reset: the level channels and their
/// worker tasks are discarded and recreated, the input buffer position is cleared, the leaf
/// index is reset to zero, and the previous root hash is discarded. The algorithm factory,
/// block size, and fan-out are fixed at construction and are not affected by reset. The
/// internal block buffer is allocated once at construction and reused across calls; its
/// contents beyond the current write position are always explicitly zeroed before hashing,
/// so no data from a previous computation can influence the next.
/// </para>
/// <para>
/// <b>Thread safety:</b> concurrent calls from multiple threads on the same instance produce
/// undefined behaviour and are not supported. The <c>ComputeHash*</c> APIs must be called
/// sequentially. If concurrent hashing is required, construct an independent instance per
/// thread or task; instances do not share any mutable state with one another.
/// </para>
/// <para>
/// <b>Diagnostics:</b> an optional <see cref="MerkleTreeDiagnostics"/> instance may be
/// supplied to each <c>ComputeHash</c> call. When present, every leaf and internal node is
/// recorded as it is produced, enabling post-computation inspection and independent hash
/// re-validation. Passing distinct instances across calls ensures each computation's trace
/// remains isolated. Recording incurs additional allocation and should not be enabled in
/// production paths.
/// </para>
/// <para>
/// <b>Shutdown contract:</b> channels are completed strictly bottom-up — level N's channel is
/// closed only after level N's worker has fully exited. The dashed "EOF · close ch L₀" box at
/// <c>t₄</c> in the Dispatcher lane closes the lowest channel first; the Level 0 worker then
/// drains, emits its final parent, and closes <c>ch L₁</c>, and so on up the tree. This ordering
/// guarantees that every node a worker promotes into level N+1 arrives before level N+1's channel
/// is closed, eliminating the lost-node race that would otherwise cause finalisation to deadlock.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/hashing.html#pattern-6--merkle-trees">Merkle-tree recipes in the hashing guide</seealso>
public sealed class ParallelMerkleTreeHash : IDisposable
{
    private readonly int _blockSize;
    private readonly int _fanOut;
    private readonly Func<HashAlgorithm> _algorithmFactory;
    private readonly CancellationTokenSource _cts = new();

    // One channel and one worker task per tree level, created lazily as the tree grows.
    // Both dictionaries are cleared and rebuilt by Reset() at the start of each computation.
    private readonly ConcurrentDictionary<int, Channel<byte[]>> _levelChannels = new();
    private readonly ConcurrentDictionary<int, Task> _levelWorkers = new();
    private readonly object _levelCreationLock = new();

    // Raw-byte accumulation buffer. Single-caller only; not thread-safe.
    // Allocated once and reused across calls; Reset() zeroes _bufferLength without clearing the
    // buffer contents, since FinalizeAsync always explicitly pads before hashing any tail block.
    private readonly byte[] _blockBuffer;
    private int _bufferLength;

    // Incremented each time a leaf is submitted; used to assign stable indices for diagnostics.
    // Only accessed from the producer thread — no atomic operation required.
    // Reset to 0 by Reset() at the start of each computation.
    private int _leafIndex;

    // Set per-call via Reset(); null when diagnostics are not requested for the current call.
    private MerkleTreeDiagnostics? _diagnostics;

    // Written by whichever level worker identifies the final surviving node as the root.
    // Cleared to null by Reset() at the start of each computation.
    private byte[]? _rootHash;
    private bool _disposed;

    // Set per-call by ComputeHash* methods; links the disposal token with any user-supplied
    // cancellation token so that level workers respond to external cancellation.
    private CancellationToken _activeToken;

    /// <summary>
    /// Initialises a new <see cref="ParallelMerkleTreeHash"/> instance with the specified hash
    /// algorithm factory, block size, and fan-out.
    /// </summary>
    /// <param name="algorithmFactory">
    ///   Factory that returns a fresh, independent <see cref="HashAlgorithm"/> on each call.
    ///   Must not be <see langword="null"/>. A distinct instance is created per hash operation so
    ///   that concurrent level workers never share algorithm state.
    /// </param>
    /// <param name="blockSize">
    ///   The size in bytes of each leaf block. Must be greater than zero. Defaults to 4096.
    /// </param>
    /// <param name="fanOut">
    ///   The number of child nodes combined into each parent node during tree reduction.
    ///   Must be at least 2. Defaults to 2. Larger values produce shallower trees but wider
    ///   internal nodes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="algorithmFactory"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="blockSize"/> is less than or equal to zero, or
    ///   <paramref name="fanOut"/> is less than 2.
    /// </exception>
    public ParallelMerkleTreeHash(
        Func<HashAlgorithm> algorithmFactory,
        int blockSize = 4096,
        int fanOut = 2)
    {
        this._algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        this._blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(
                                                        nameof(blockSize),
                                                        string.Format(ResourceStrings.ArgumentOutOfRangeException_BlockSizeMustBeGreaterThan, 0));
        this._fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), "Fan-out must be at least 2.");
        this._blockBuffer = new byte[blockSize];
    }

    /// <summary>
    /// Asynchronously reads <paramref name="input"/> in fixed-size blocks, feeds leaf hashes into
    /// the tree pipeline, and returns the Merkle root hash once all data has been processed.
    /// </summary>
    /// <param name="input">The readable stream to hash. Must not be <see langword="null"/>.</param>
    /// <param name="diagnostics">
    ///   An optional <see cref="MerkleTreeDiagnostics"/> instance that records each node produced
    ///   during this computation. Pass <see langword="null"/> to disable diagnostic recording.
    ///   Supplying a fresh instance per call ensures each computation's trace is isolated.
    /// </param>
    /// <param name="cancellationToken">
    ///   Token used to cancel the operation. When signalled, the read loop is stopped and all
    ///   active level workers are drained before the exception is propagated.
    /// </param>
    /// <returns>
    ///   A byte array containing the Merkle root hash of all data read from <paramref name="input"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="input"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">No input data was provided.</exception>
    /// <exception cref="OperationCanceledException">
    ///   <paramref name="cancellationToken"/> was cancelled before all data could be read.
    /// </exception>
    public async Task<byte[]> ComputeHashAsync(
        Stream input,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ObjectDisposedException.ThrowIf(this._disposed, this);

        var linked = CancellationTokenSource.CreateLinkedTokenSource(this._cts.Token, cancellationToken);
        try
        {
            this.Reset(diagnostics, linked.Token);

            // Read in chunks larger than one block so that a single ReadAsync can feed several leaves,
            // keeping the I/O system ahead of the hashing pipeline.
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(this._blockSize * 8);
            try
            {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), linked.Token)) > 0)
                    this.ProcessBytes(readBuffer.AsSpan(0, bytesRead));
            }
            catch (Exception)
            {
                await this.DrainWorkersAsync();
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer, clearArray: true);
            }

            return await this.FinalizeAsync();
        }
        finally
        {
            linked.Dispose();
        }
    }

    /// <summary>
    /// Synchronously computes the Merkle root hash of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The byte span to hash.</param>
    /// <param name="diagnostics">
    ///   An optional <see cref="MerkleTreeDiagnostics"/> instance that records each node produced
    ///   during this computation. Pass <see langword="null"/> to disable diagnostic recording.
    /// </param>
    /// <returns>
    ///   A byte array containing the Merkle root hash of <paramref name="data"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">No input data was provided.</exception>
    public byte[] ComputeHash(ReadOnlySpan<byte> data, MerkleTreeDiagnostics? diagnostics = null)
    {
        this.Reset(diagnostics, this._cts.Token);
        this.ProcessBytes(data);
        return this.FinalizeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously computes the Merkle root hash of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null"/>.</param>
    /// <param name="diagnostics">
    ///   An optional <see cref="MerkleTreeDiagnostics"/> instance that records each node produced
    ///   during this computation. Pass <see langword="null"/> to disable diagnostic recording.
    /// </param>
    /// <returns>
    ///   A byte array containing the Merkle root hash of <paramref name="data"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">No input data was provided.</exception>
    public byte[] ComputeHash(byte[] data, MerkleTreeDiagnostics? diagnostics = null) =>
        this.ComputeHash(new ReadOnlySpan<byte>(data), diagnostics);

    /// <summary>
    /// Synchronously computes the Merkle root hash of a region within <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The source byte array. Must not be <see langword="null"/>.</param>
    /// <param name="offset">The zero-based index at which to begin reading.</param>
    /// <param name="count">The number of bytes to hash.</param>
    /// <param name="diagnostics">
    ///   An optional <see cref="MerkleTreeDiagnostics"/> instance that records each node produced
    ///   during this computation. Pass <see langword="null"/> to disable diagnostic recording.
    /// </param>
    /// <returns>
    ///   A byte array containing the Merkle root hash of the specified region.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="data"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="offset"/> or <paramref name="count"/> is negative, or
    ///   <paramref name="offset"/> + <paramref name="count"/> exceeds the length of <paramref name="data"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">No input data was provided.</exception>
    public byte[] ComputeHash(byte[] data, int offset, int count, MerkleTreeDiagnostics? diagnostics = null) =>
        this.ComputeHash(new ReadOnlySpan<byte>(data, offset, count), diagnostics);

    // -----------------------------------------------------------------------------------------
    // Reset — restores the instance to a clean state for the next computation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Resets all per-computation state so the instance can be reused for a new hash operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clears the level channel and worker dictionaries, resets the input buffer position, leaf
    /// index, root hash, and active diagnostics reference, then recreates the level-0 channel and
    /// its worker so the producer can begin writing immediately.
    /// </para>
    /// <para>
    /// This method is always the first thing called by every public <c>ComputeHash</c> overload,
    /// even on the very first use, which avoids the need for any eager initialisation in the
    /// constructor.
    /// </para>
    /// </remarks>
    /// <param name="diagnostics">The diagnostics sink to capture per-level timings, or <see langword="null" />.</param>
    /// <param name="activeToken">The cancellation token associated with the current hashing session.</param>
    private void Reset(MerkleTreeDiagnostics? diagnostics, CancellationToken activeToken)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        // Discard all channels and workers from the previous computation. By the time Reset is
        // called, FinalizeAsync has already awaited every worker to completion, so clearing these
        // dictionaries releases the completed tasks without risking a concurrent write.
        this._levelChannels.Clear();
        this._levelWorkers.Clear();

        this._bufferLength = 0;
        this._leafIndex = 0;
        this._rootHash = null;
        this._diagnostics = diagnostics;
        this._activeToken = activeToken;

        // Recreate level 0 immediately so the producer can submit leaves without waiting.
        this.EnsureLevelExists(0);
    }

    // -----------------------------------------------------------------------------------------
    // Input ingestion
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Fills <see cref="_blockBuffer"/> from <paramref name="data"/>, flushing a leaf hash to
    /// level 0 each time the buffer reaches <see cref="_blockSize"/>.
    /// </summary>
    /// <param name="data">The input bytes to feed into the current leaf accumulator.</param>
    private void ProcessBytes(ReadOnlySpan<byte> data)
    {
        while (!data.IsEmpty)
        {
            int toWrite = Math.Min(this._blockSize - this._bufferLength, data.Length);
            data.Slice(0, toWrite).CopyTo(this._blockBuffer.AsSpan(this._bufferLength));
            this._bufferLength += toWrite;
            data = data.Slice(toWrite);

            if (this._bufferLength == this._blockSize)
            {
                this.SubmitLeaf(this._blockBuffer, this._blockSize);
                this._bufferLength = 0;
            }
        }
    }

    /// <summary>
    /// Hashes <paramref name="length"/> bytes from <paramref name="data"/>, records the leaf node
    /// if diagnostics are enabled, and submits the hash to level 0.
    /// </summary>
    /// <param name="data">The leaf buffer (owned by the caller until consumed).</param>
    /// <param name="length">The number of valid bytes in <paramref name="data" />.</param>
    private void SubmitLeaf(byte[] data, int length)
    {
        var hash = this.HashSpan(data.AsSpan(0, length));
        this._diagnostics?.RecordLeaf(this._leafIndex, hash);
        this.WriteToLevel(0, hash);
        this._leafIndex++;
    }

    // -----------------------------------------------------------------------------------------
    // Level management
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Appends <paramref name="hash" /> to the intermediate buffer at the specified tree
    /// <paramref name="level" />, creating the level's buffer lazily if it does not yet exist.
    /// </summary>
    /// <param name="level">The tree level (0 for leaves, incrementing upward).</param>
    /// <param name="hash">The hash bytes to append.</param>
    private void WriteToLevel(int level, byte[] hash)
    {
        // TryWrite on an unbounded channel fails only if the channel has already been completed.
        // Under the correct bottom-up shutdown sequence this path must never be reached.
        if (!this._levelChannels[level].Writer.TryWrite(hash))
            throw new InvalidOperationException(
                $"Write to level-{level} channel failed. The channel was completed before all nodes were submitted.");
    }

    /// <summary>
    /// Grows the internal level buffer list to include <paramref name="level" />, allocating
    /// fresh buffers for any intermediate levels.
    /// </summary>
    /// <param name="level">The zero-based tree level that must be addressable.</param>
    private void EnsureLevelExists(int level)
    {
        // Fast path — no lock required once the entry is visible in the dictionary.
        if (this._levelChannels.ContainsKey(level))
            return;

        lock (this._levelCreationLock)
        {
            if (this._levelChannels.ContainsKey(level))
                return;

            // SingleReader  = this level's worker.
            // SingleWriter  = the level below's worker (or the producer thread for level 0).
            // AllowSynchronousContinuations = false prevents a writer from inadvertently
            //   driving the reader's continuation inline, which could overflow the stack in
            //   deep trees with small fan-out.
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            };

            var channel = Channel.CreateUnbounded<byte[]>(options);
            this._levelChannels[level] = channel;
            this._levelWorkers[level] = Task.Run(() => this.RunLevelWorkerAsync(level, channel, this._activeToken));
        }
    }

    // -----------------------------------------------------------------------------------------
    // Per-level worker
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Reads hash nodes from <paramref name="channel"/>, groups them by <see cref="_fanOut"/>, and
    /// promotes each full group as a combined parent hash to the next level. Any remainder after
    /// the channel closes is resolved according to the following rules:
    /// </summary>
    /// <remarks>
    /// <para>
    /// Zero remainder: every node was promoted in a full group. The final result will emerge
    /// naturally from a higher level worker.
    /// </para>
    /// <para>
    /// One node with no higher level yet existing: no full group was ever promoted from this
    /// level, making this the topmost level. The single surviving node is the Merkle root and is
    /// assigned directly without re-hashing, consistent with the single-threaded implementation.
    /// </para>
    /// <para>
    /// Anything else: a partial group (or a single node whose level already has a higher peer)
    /// is combined and promoted upward. The next level's worker repeats this logic until a root
    /// is identified.
    /// </para>
    /// </remarks>
    /// <param name="level">The tree level this worker serves.</param>
    /// <param name="channel">The bounded channel producing input hashes for this level.</param>
    /// <param name="token">The cancellation token that stops the worker.</param>
    /// <returns>A task that completes when the worker has drained its channel.</returns>
    private async Task RunLevelWorkerAsync(int level, Channel<byte[]> channel, CancellationToken token)
    {
        var pending = new List<byte[]>(this._fanOut);
        int parentIndex = 0;

        // Drain the channel. The worker below runs concurrently, so tree reduction at this
        // level overlaps with continued leaf production and lower-level promotion.
        await foreach (var hash in channel.Reader.ReadAllAsync(token))
        {
            pending.Add(hash);

            if (pending.Count == this._fanOut)
            {
                var parentHash = this.CombineAndHash(pending, level, parentIndex);
                parentIndex++;
                pending.Clear();
                this.EnsureLevelExists(level + 1);
                this.WriteToLevel(level + 1, parentHash);
            }
        }

        // The channel is now complete — no further nodes will arrive at this level.
        switch (pending.Count)
        {
            case 0:
                // All nodes were promoted in full groups; nothing left to handle here.
                break;

            case 1 when !this._levelChannels.ContainsKey(level + 1):
                // Single surviving node with no higher level: this is the Merkle root.
                this._rootHash = pending[0];
                break;

            default:
                // Partial group, or a lone node alongside a pre-existing higher level:
                // combine and promote so the next worker can make the root determination.
                var remainderHash = this.CombineAndHash(pending, level, parentIndex);
                this.EnsureLevelExists(level + 1);
                this.WriteToLevel(level + 1, remainderHash);
                break;
        }
    }

    // -----------------------------------------------------------------------------------------
    // Finalisation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Flushes any remaining partial block, then sequentially closes each level's channel and
    /// awaits its worker before advancing to the next level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sequential bottom-up shutdown is the core correctness invariant. Level N's channel is
    /// completed only after level N's worker has fully exited. Because level N's worker is the
    /// sole writer to level N+1's channel, this ordering guarantees that every promotion into
    /// level N+1 has landed before level N+1 is closed. The loop terminates naturally when
    /// the next level has never been created, meaning no further promotions occurred.
    /// </para>
    /// </remarks>
    /// <returns>The Merkle root hash.</returns>
    /// <exception cref="InvalidOperationException">No input data was provided.</exception>
    private async Task<byte[]> FinalizeAsync()
    {
        // Zero-pad the partial tail block to a full block size before hashing, so that every
        // leaf is the same width regardless of input alignment. The bytes beyond _bufferLength
        // in _blockBuffer are cleared explicitly since the buffer is reused across calls.
        if (this._bufferLength > 0)
        {
            Array.Clear(this._blockBuffer, this._bufferLength, this._blockSize - this._bufferLength);
            this.SubmitLeaf(this._blockBuffer, this._blockSize);
            this._bufferLength = 0;
        }

        // Complete → await → advance: each iteration closes one level and waits for its
        // worker to finish all promotions before the next level's channel is closed.
        for (int level = 0; this._levelChannels.TryGetValue(level, out var channel); level++)
        {
            channel.Writer.Complete();
            await this._levelWorkers[level];
        }

        return this._rootHash ?? throw new InvalidOperationException("No input data was provided.");
    }

    /// <summary>
    /// Completes all level channels and awaits their workers, suppressing any exceptions that
    /// arise from cancelled tokens or completed channels during cancellation teardown.
    /// </summary>
    /// <returns>A task that completes when every level worker has drained and exited.</returns>
    private async Task DrainWorkersAsync()
    {
        foreach (var channel in this._levelChannels.Values)
            channel.Writer.TryComplete();

        foreach (var worker in this._levelWorkers.Values)
        {
            try { await worker; }
            catch { }
        }
    }

    // -----------------------------------------------------------------------------------------
    // Hashing helpers
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Hashes <paramref name="data"/> using a fresh algorithm instance.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="HashAlgorithm.TryComputeHash"/> to operate directly on the span,
    /// avoiding an intermediate heap copy of the input bytes.
    /// </remarks>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The hash computed by a freshly-created <see cref="HashAlgorithm" />.</returns>
    private byte[] HashSpan(ReadOnlySpan<byte> data)
    {
        using var hasher = this._algorithmFactory();
        var result = new byte[hasher.HashSize / 8];
        if (!hasher.TryComputeHash(data, result, out _))
            throw new CryptographicException("TryComputeHash returned false; the output buffer may be too small.");
        return result;
    }

    /// <summary>
    /// Combines and hashes a list of child hash values using <see cref="HashAlgorithm.TransformBlock"/>,
    /// records the resulting node in diagnostics if enabled, and returns the parent hash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each child hash is fed sequentially into the algorithm's accumulation state via
    /// <see cref="HashAlgorithm.TransformBlock"/>, avoiding any intermediate allocation for
    /// the concatenated bytes.
    /// </para>
    /// <para>
    /// When diagnostics are active, a snapshot of the child hashes is captured and recorded
    /// alongside the result in a single guarded block. This keeps the snapshot and the recording
    /// co-located, making their shared nullability condition structurally obvious rather than
    /// relying on a null-forgiving operator across separate statements.
    /// </para>
    /// </remarks>
    /// <param name="hashes">The child hashes to combine.</param>
    /// <param name="sourceLevel">The level from which the children came.</param>
    /// <param name="parentIndex">The index of the resulting parent node within level <paramref name="sourceLevel"/> + 1.</param>
    /// <param name="hashes">The child hashes being combined.</param>
    /// <param name="sourceLevel">The tree level that produced <paramref name="hashes" />.</param>
    /// <param name="parentIndex">The zero-based index of the parent node being computed.</param>
    /// <returns>The combined parent hash.</returns>
    private byte[] CombineAndHash(List<byte[]> hashes, int sourceLevel, int parentIndex)
    {
        using var hasher = this._algorithmFactory();

        // Feed all but the last child via TransformBlock — purely state accumulation, no output.
        for (int i = 0; i < hashes.Count - 1; i++)
            hasher.TransformBlock(hashes[i], 0, hashes[i].Length, null, 0);

        // TransformFinalBlock finalises accumulation and populates hasher.Hash.
        hasher.TransformFinalBlock(hashes[^1], 0, hashes[^1].Length);

        var result = hasher.Hash!;

        // Snapshot child hashes and record the node only when diagnostics are active.
        // Keeping the snapshot and the recording in the same guarded block makes the
        // nullability relationship self-evident and avoids any suppression operator.
        if (this._diagnostics is not null)
        {
            var childSnapshots = hashes.ConvertAll(static h => (byte[])h.Clone()).ToArray();
            this._diagnostics.RecordInternal(sourceLevel + 1, parentIndex, childSnapshots, result);
        }

        return result;
    }

    // -----------------------------------------------------------------------------------------
    // Disposal
    // -----------------------------------------------------------------------------------------

    /// <inheritdoc />
    public void Dispose()
    {
        if (this._disposed) return;
        this._disposed = true;
        this._cts.Cancel();
        this._cts.Dispose();
    }
}
