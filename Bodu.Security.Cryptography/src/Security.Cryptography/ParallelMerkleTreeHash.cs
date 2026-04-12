using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a parallel Merkle tree hash implementation using a concurrent level-worker pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Input bytes are divided into fixed-size blocks. Each block is hashed independently to form a
    /// leaf node at level 0. A dedicated async worker per tree level groups incoming nodes by
    /// <c>fanOut</c>, hashes each full group into a parent node, and writes it to the next level's
    /// channel. Workers at adjacent levels run concurrently, so tree reduction overlaps with
    /// continued leaf production.
    /// </para>
    /// <para>
    /// <b>Shutdown contract:</b> channels are completed strictly bottom-up — level N's channel is
    /// closed only after level N's worker has fully exited. This guarantees that every node a worker
    /// promotes into level N+1 arrives before level N+1's channel is closed, eliminating the
    /// lost-node race that would otherwise cause finalisation to deadlock.
    /// </para>
    /// <para>
    /// <b>Diagnostics:</b> an optional <see cref="MerkleTreeDiagnostics"/> instance may be supplied
    /// at construction. When present, every leaf and internal node is recorded as it is produced,
    /// enabling post-computation inspection and independent hash re-validation. Recording incurs
    /// additional allocation and should not be enabled in production paths.
    /// </para>
    /// <para>
    /// <b>Single-use:</b> each instance is intended for one hash computation. Construct a new
    /// instance for each input. The <c>ComputeHash*</c> APIs are not thread-safe and must not be
    /// called concurrently.
    /// </para>
    /// </remarks>
    public sealed class ParallelMerkleTreeHash : IDisposable
    {
        private readonly int _blockSize;
        private readonly int _fanOut;
        private readonly Func<HashAlgorithm> _algorithmFactory;
        private readonly MerkleTreeDiagnostics? _diagnostics;
        private readonly CancellationTokenSource _cts = new();

        // One channel and one worker task per tree level, created lazily as the tree grows.
        // Each channel has a single writer (the level below's worker, or the producer for level 0)
        // and a single reader (its own worker), so UnboundedChannelOptions can be tuned accordingly.
        private readonly ConcurrentDictionary<int, Channel<byte[]>> _levelChannels = new();
        private readonly ConcurrentDictionary<int, Task> _levelWorkers = new();
        private readonly object _levelCreationLock = new();

        // Raw-byte accumulation buffer. Single-caller only; not thread-safe.
        private readonly byte[] _blockBuffer;
        private int _bufferLength;

        // Incremented each time a leaf is submitted; used to assign stable indices for diagnostics.
        // Only accessed from the producer thread — no atomic operation required.
        private int _leafIndex;

        // Written by whichever level worker identifies the final surviving node as the root.
        private byte[]? _rootHash;
        private bool _disposed;

        /// <summary>
        /// Initialises a new <see cref="ParallelMerkleTreeHash"/> instance with the specified hash
        /// algorithm factory, block size, fan-out, and optional diagnostics.
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
        /// <param name="diagnostics">
        ///   An optional <see cref="MerkleTreeDiagnostics"/> instance that records each node as it
        ///   is produced. Pass <see langword="null"/> to disable diagnostic recording.
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
            int fanOut = 2,
            MerkleTreeDiagnostics? diagnostics = null)
        {
            _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
            _blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");
            _fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), "Fan-out must be at least 2.");
            _diagnostics = diagnostics;
            _blockBuffer = new byte[blockSize];

            // Level 0 is always required; create it eagerly so the producer can write immediately.
            EnsureLevelExists(0);
        }

        /// <summary>
        /// Asynchronously reads <paramref name="input"/> in fixed-size blocks, feeds leaf hashes into
        /// the tree pipeline, and returns the Merkle root hash once all data has been processed.
        /// </summary>
        /// <param name="input">The readable stream to hash. Must not be <see langword="null"/>.</param>
        /// <param name="cancellationToken">
        ///   Token used to cancel the read loop. Cancellation stops further reads but does not
        ///   interrupt level workers already in progress; those are cancelled via <see cref="Dispose"/>.
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
        public async Task<byte[]> ComputeHashAsync(Stream input, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(input);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

            // Read in chunks larger than one block so that a single ReadAsync can feed several leaves,
            // keeping the I/O system ahead of the hashing pipeline.
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(_blockSize * 8);
            try
            {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), linked.Token)) > 0)
                    ProcessBytes(readBuffer.AsSpan(0, bytesRead));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer, clearArray: true);
            }

            return await FinalizeAsync();
        }

        /// <summary>
        /// Synchronously computes the Merkle root hash of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The byte span to hash.</param>
        /// <returns>
        ///   A byte array containing the Merkle root hash of <paramref name="data"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        /// <exception cref="InvalidOperationException">No input data was provided.</exception>
        public byte[] ComputeHash(ReadOnlySpan<byte> data)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ProcessBytes(data);
            return FinalizeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously computes the Merkle root hash of a region within <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The source byte array. Must not be <see langword="null"/>.</param>
        /// <param name="offset">The zero-based index at which to begin reading.</param>
        /// <param name="count">The number of bytes to hash.</param>
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
        public byte[] ComputeHash(byte[] data, int offset, int count) =>
            ComputeHash(new ReadOnlySpan<byte>(data, offset, count));

        /// <inheritdoc cref="ComputeHash(ReadOnlySpan{byte})"/>
        public byte[] ComputeHash(byte[] data) =>
            ComputeHash(new ReadOnlySpan<byte>(data));

        // -----------------------------------------------------------------------------------------
        // Input ingestion
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Fills <see cref="_blockBuffer"/> from <paramref name="data"/>, flushing a leaf hash to
        /// level 0 each time the buffer reaches <see cref="_blockSize"/>.
        /// </summary>
        private void ProcessBytes(ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                int toWrite = Math.Min(_blockSize - _bufferLength, data.Length);
                data.Slice(0, toWrite).CopyTo(_blockBuffer.AsSpan(_bufferLength));
                _bufferLength += toWrite;
                data = data.Slice(toWrite);

                if (_bufferLength == _blockSize)
                {
                    SubmitLeaf(_blockBuffer, _blockSize);
                    _bufferLength = 0;
                }
            }
        }

        /// <summary>
        /// Hashes <paramref name="length"/> bytes from <paramref name="data"/>, records the leaf node
        /// if diagnostics are enabled, and submits the hash to level 0.
        /// </summary>
        private void SubmitLeaf(byte[] data, int length)
        {
            var hash = HashSpan(data.AsSpan(0, length));
            _diagnostics?.RecordLeaf(_leafIndex, hash);
            WriteToLevel(0, hash);
            _leafIndex++;
        }

        // -----------------------------------------------------------------------------------------
        // Level management
        // -----------------------------------------------------------------------------------------

        private void WriteToLevel(int level, byte[] hash)
        {
            // TryWrite on an unbounded channel fails only if the channel has already been completed.
            // Under the correct bottom-up shutdown sequence this path must never be reached.
            if (!_levelChannels[level].Writer.TryWrite(hash))
                throw new InvalidOperationException(
                    $"Write to level-{level} channel failed. The channel was completed before all nodes were submitted.");
        }

        private void EnsureLevelExists(int level)
        {
            // Fast path — no lock required once the entry is visible in the dictionary.
            if (_levelChannels.ContainsKey(level))
                return;

            lock (_levelCreationLock)
            {
                if (_levelChannels.ContainsKey(level))
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
                _levelChannels[level] = channel;
                _levelWorkers[level] = Task.Run(() => RunLevelWorkerAsync(level, channel, _cts.Token));
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
        private async Task RunLevelWorkerAsync(int level, Channel<byte[]> channel, CancellationToken token)
        {
            var pending = new List<byte[]>(_fanOut);
            int parentIndex = 0;

            // Drain the channel. The worker below runs concurrently, so tree reduction at this
            // level overlaps with continued leaf production and lower-level promotion.
            await foreach (var hash in channel.Reader.ReadAllAsync(token))
            {
                pending.Add(hash);

                if (pending.Count == _fanOut)
                {
                    var parentHash = CombineAndHash(pending, level, parentIndex);
                    parentIndex++;
                    pending.Clear();
                    EnsureLevelExists(level + 1);
                    WriteToLevel(level + 1, parentHash);
                }
            }

            // The channel is now complete — no further nodes will arrive at this level.
            switch (pending.Count)
            {
                case 0:
                    // All nodes were promoted in full groups; nothing left to handle here.
                    break;

                case 1 when !_levelChannels.ContainsKey(level + 1):
                    // Single surviving node with no higher level: this is the Merkle root.
                    _rootHash = pending[0];
                    break;

                default:
                    // Partial group, or a lone node alongside a pre-existing higher level:
                    // combine and promote so the next worker can make the root determination.
                    var remainderHash = CombineAndHash(pending, level, parentIndex);
                    EnsureLevelExists(level + 1);
                    WriteToLevel(level + 1, remainderHash);
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
            if (_bufferLength > 0)
            {
                Array.Clear(_blockBuffer, _bufferLength, _blockSize - _bufferLength);
                SubmitLeaf(_blockBuffer, _blockSize);
                _bufferLength = 0;
            }

            // Complete → await → advance: each iteration closes one level and waits for its
            // worker to finish all promotions before the next level's channel is closed.
            for (int level = 0; _levelChannels.TryGetValue(level, out var channel); level++)
            {
                channel.Writer.Complete();
                await _levelWorkers[level];
            }

            return _rootHash ?? throw new InvalidOperationException("No input data was provided.");
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
        private byte[] HashSpan(ReadOnlySpan<byte> data)
        {
            using var hasher = _algorithmFactory();
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
        private byte[] CombineAndHash(List<byte[]> hashes, int sourceLevel, int parentIndex)
        {
            using var hasher = _algorithmFactory();

            // Feed all but the last child via TransformBlock — purely state accumulation, no output.
            for (int i = 0; i < hashes.Count - 1; i++)
                hasher.TransformBlock(hashes[i], 0, hashes[i].Length, null, 0);

            // TransformFinalBlock finalises accumulation and populates hasher.Hash.
            hasher.TransformFinalBlock(hashes[^1], 0, hashes[^1].Length);

            var result = hasher.Hash!;

            // Snapshot child hashes and record the node only when diagnostics are active.
            // Keeping the snapshot and the recording in the same guarded block makes the
            // nullability relationship self-evident and avoids any suppression operator.
            if (_diagnostics is not null)
            {
                var childSnapshots = hashes.ConvertAll(static h => (byte[])h.Clone()).ToArray();
                _diagnostics.RecordInternal(sourceLevel + 1, parentIndex, childSnapshots, result);
            }

            return result;
        }

        // -----------------------------------------------------------------------------------------
        // Disposal
        // -----------------------------------------------------------------------------------------

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}