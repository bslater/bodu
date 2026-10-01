// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 256-bit cryptographic hash using the <c>BLAKE3</c> algorithm designed by Jack O'Connor, Jean-Philippe
/// Aumasson, Samuel Neves, and Zooko Wilcox-O'Hearn. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE3 is a cryptographic hash function that combines the speed of non-cryptographic hashes with strong security
/// guarantees. It is based on a binary tree structure where each leaf (chunk) processes up to 1024 bytes of input and
/// each internal (parent) node combines two child chaining values. All compression is performed by a single ARX-based
/// function derived from the BLAKE2 and ChaCha families.
/// </para>
/// <para>
/// Input is divided into 1024-byte chunks, each compressed block-by-block into an 8-word (256-bit) chaining value. When
/// more than one chunk exists the chaining values are folded pairwise into parent nodes until a single root chaining
/// value remains. The root compression call is distinguished by the <c>ROOT</c> domain-separation flag, which enables
/// XOF-style output extraction; this implementation fixes the output length at 256 bits.
/// </para>
/// <para>
/// This implementation inherits its 64-byte residual buffer, running byte counter, and defer-on-full-block buffering
/// loop from <see cref="DeferredFinalBlockHashAlgorithm" />. The final 64-byte block is not compressed until
/// <see cref="HashAlgorithm.HashFinal" /> is called, ensuring that chunk-level and tree-level domain flags can be
/// applied correctly.
/// </para>
/// <para>
/// This implementation supports the standard, unkeyed hash mode only. Keyed-hash and key-derivation modes are not
/// exposed.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 256 bits (32 bytes), fixed.</description>
/// </item>
/// <item>
/// <description>Block size: 64 bytes; chunk size: 1024 bytes (the leaf of the hash tree).</description>
/// </item>
/// <item>
/// <description>
/// Construction: binary Merkle tree over chunks, ARX compression derived from BLAKE2 / ChaCha.
/// </description>
/// </item>
/// <item>
/// <description>Mode: standard unkeyed hash only - keyed hash and KDF modes are not exposed.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose BLAKE3.</strong> Reach for BLAKE3 when raw throughput on long inputs is the priority - its
/// tree structure is naturally parallel-friendly and outperforms <see cref="Blake2b" />, SHA-2, and SHA-3 on
/// multi-megabyte messages. For short inputs the difference shrinks and any of the BLAKE2 / SHA-2 variants is fine. Use
/// <see cref="Blake2b" /> if a configurable output size or RFC 7693-compatible MAC mode is required; use
/// <see cref="MerkleTree" /> if you want RFC 6962's tree, its proofs, and explicit control over the block size and the
/// underlying leaf hash.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var blake3 = new Blake3();
/// byte[] digest = blake3.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Blake2b"/> <seealso cref="Blake2s"/>
public sealed class Blake3
    : DeferredFinalBlockHashAlgorithm
{
    /// <summary>Size, in bytes, of a single compression input block.</summary>
    private new const int BlockSize = Blake3Core.BlockBytes;

    /// <summary>Size, in bytes, of a single input chunk (leaf of the hash tree).</summary>
    private const int ChunkSize = Blake3Core.ChunkBytes;

    /// <summary>Maximum possible depth of the chaining-value stack. BLAKE3 supports up to <c>2^54</c> chunks per message, so the Merkle tree height is bounded at 54 - any well-formed input fits within this bound.</summary>
    private const int MaxCvStackDepth = 54;

    /// <summary>Output length in bytes.</summary>
    private const int OutLen = 32;

    /// <summary>The most subtrees one write is divided into. A write of under 2 GiB needs at most 42: up to 21 that complete the subtrees earlier writes left open, and up to 21 more, one per set bit of its remaining chunk count.</summary>
    private const int MaxSubtrees = 64;

    /// <summary>Running chaining value for the chunk currently being compressed.</summary>
    /// <remarks>
    /// Reset to the IV at the start of each new chunk (when the first block of a chunk is processed) and updated in
    /// place after every compression call. Carries the accumulated chaining state block-by-block until the chunk
    /// completes.
    /// </remarks>
    private readonly uint[] _chunkCv = new uint[8];

    /// <summary>Chaining-value stack used to build parent nodes as chunks complete. Laid out as a flat 8-word slice per level, indexed by <see cref="_cvStackDepth" />, so per-level pushes and merges run without per-level array allocations.</summary>
    private readonly uint[] _cvStack = new uint[MaxCvStackDepth * 8];

    /// <summary>The greatest number of threads one write may use; see <see cref="MaxDegreeOfParallelism" />.</summary>
    private readonly int _maxDegreeOfParallelism;

    /// <summary>Current depth of <see cref="_cvStack" /> - the number of 8-word CV slices currently live, with the active top slice occupying words <c>[(_cvStackDepth - 1) * 8, _cvStackDepth * 8)</c> when non-zero.</summary>
    private int _cvStackDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake3" /> class, configured to produce a 256-bit digest.
    /// </summary>
    /// <remarks>
    /// Every hash runs on the calling thread; see <see cref="MaxDegreeOfParallelism" />.
    /// </remarks>
    public Blake3()
        : this(maxDegreeOfParallelism: 1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake3" /> class, configured to produce a 256-bit digest, with the
    /// specified bound on the threads each write may use.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of threads one write may use, the calling thread included; <c>-1</c> for up to one per
    /// processor.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxDegreeOfParallelism" /> is zero or less than <c>-1</c>.
    /// </exception>
    /// <remarks>
    /// The digest never depends on the bound; see <see cref="MaxDegreeOfParallelism" />.
    /// </remarks>
    public Blake3(int maxDegreeOfParallelism)
        : base(BlockSize * 8)
    {
        CryptographyThrowHelper.ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        HashSizeValue = 256;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        Blake3Core.InitializationVector.CopyTo(_chunkCv);
    }

    /// <inheritdoc />
    /// <remarks>
    /// BLAKE3 has a fixed 256-bit default output; the published name is simply <c>"BLAKE3"</c>.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return "BLAKE3";
        }
    }

    /// <summary>
    /// Gets the greatest number of threads one write may use to hash its input.
    /// </summary>
    /// <value>
    /// <c>1</c>, the default, hashes every write on the calling thread. A larger value, or <c>-1</c> for up to one
    /// thread per processor, lets the whole chunks of a large write be hashed on several threads at once. The digest
    /// never depends on this value.
    /// </value>
    /// <remarks>
    /// <para>
    /// BLAKE3's tree makes every complete subtree independent of the others, so a large write is divided into parts of
    /// 64 KiB, claimed by the calling thread and the workers as each finishes the last, and the parts' chaining values
    /// are joined on the calling thread. A write of less than 256 KiB of whole chunks stays on the calling thread,
    /// where waking other threads would cost more than they save.
    /// </para>
    /// <para>
    /// The default is <c>1</c> because a service that hashes many inputs at once already keeps every core busy, and
    /// would only add hand-offs. Raise it to hash one large input faster: a file, a download, a snapshot.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int MaxDegreeOfParallelism
    {
        get
        {
            ThrowIfDisposed();
            return _maxDegreeOfParallelism;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this transform instance can be reused after a hash operation is completed.
    /// </summary>
    /// <value>
    /// <see langword="true" />; <see cref="Blake3" /> resets its state automatically and may be reused across multiple
    /// <c>ComputeHash</c> calls.
    /// </value>
    public override bool CanReuseTransform => true;

    /// <summary>
    /// Gets a value indicating whether multiple blocks may be transformed in a single
    /// <see cref="HashAlgorithm.TransformBlock" /> call.
    /// </summary>
    /// <value><see langword="true" />; the implementation accumulates arbitrary-length input internally.</value>
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// Clears the CV stack and restores <see cref="_chunkCv" /> to the BLAKE3 initialization vector, ready for a new
    /// chunk. The inherited residual buffer and counters are cleared by the base call (which also throws
    /// <see cref="ObjectDisposedException" /> if the instance has been disposed).
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        CryptographyHelper.Clear(_cvStack);
        _cvStackDepth = 0;
        Blake3Core.InitializationVector.CopyTo(_chunkCv);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Clears the CV stack, zeroes <see cref="_chunkCv" />, releases the framework
    /// <see cref="HashAlgorithm.HashValue" /> array, and zeroes <see cref="HashAlgorithm.HashSizeValue" />. The
    /// inherited residual buffer is cleared by the base implementation when <see cref="Dispose(bool)" /> delegates to
    /// <c>base.Dispose(disposing)</c>.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            // Zero the flat CV stack buffer in one call so the secret-derived chaining values produced during
            // streaming do not survive in heap memory until the GC collects.
            CryptographyHelper.Clear(_cvStack);
            _cvStackDepth = 0;

            CryptographyHelper.Clear(_chunkCv);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Consumes the supplied input span. Whole chunks that start on a chunk boundary and are followed by more input are
    /// hashed as complete subtrees, many chunks at once; everything else goes through the block-by-block path of the
    /// base class, which keeps the final block deferred.
    /// </summary>
    /// <param name="source">
    /// The input bytes to consume. May be empty, partial, exact-block, or multi-chunk in length.
    /// </param>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();

        ulong position = _totalBytes + (ulong)_residualBytes;
        int toBoundary = (int)((ChunkSize - (position % ChunkSize)) % ChunkSize);
        if (source.Length - toBoundary > ChunkSize)
        {
            base.HashCore(source[..toBoundary]);
            source = source[toBoundary..];

            // A full residual block ends the current chunk. More input follows, so it is not the final block: compress
            // it now, as the base class would before taking the next byte.
            if (_residualBytes == BlockSize)
            {
                ProcessBlock(_residualBlock.Span, _totalBytes + BlockSize, isFinal: false);
                _totalBytes += BlockSize;
                _residualBytes = 0;
            }

            source = source[HashSubtrees(source)..];
        }

        base.HashCore(source);
    }

    /// <summary>
    /// Advances the BLAKE3 compression state by one 64-byte block, applying the correct chunk-level and tree-level
    /// domain flags derived from <paramref name="totalBytesIncludingThisBlock" />.
    /// </summary>
    /// <param name="block">
    /// The 64-byte block to compress. Zero-padded by the base class when <paramref name="isFinal" /> is
    /// <see langword="true" /> and the final message byte count is not a multiple of 64.
    /// </param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count including the bytes in this block. Used to derive the chunk index, the block position
    /// within the chunk, and the true block length for the final block.
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> for the last compression call, raised by <see cref="HashAlgorithm.HashFinal" />;
    /// otherwise <see langword="false" />.
    /// </param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Derive chunk position. Subtracting 1 maps [1, 64] → block 0 of chunk 0, etc.
        // The zero guard handles the empty-input case where totalBytes is 0.
        ulong adjustedTotal = totalBytesIncludingThisBlock == 0 ? 0UL : totalBytesIncludingThisBlock - 1;
        ulong chunkIndex = adjustedTotal / (ulong)ChunkSize;
        bool isFirstBlock = (adjustedTotal % (ulong)ChunkSize) / (ulong)BlockSize == 0;
        bool isLastBlock = totalBytesIncludingThisBlock % (ulong)ChunkSize == 0 || isFinal;

        // Non-final blocks are always full; the final block carries the true byte count.
        uint blockLen;
        if (!isFinal)
        {
            blockLen = (uint)BlockSize;
        }
        else if (totalBytesIncludingThisBlock == 0)
        {
            blockLen = 0u;
        }
        else
        {
            ulong rem = totalBytesIncludingThisBlock % (ulong)BlockSize;
            blockLen = (uint)(rem == 0 ? BlockSize : (int)rem);
        }

        // Each new chunk begins from the IV.
        if (isFirstBlock)
            Blake3Core.InitializationVector.CopyTo(_chunkCv);

        uint flags = 0u;
        if (isFirstBlock) flags |= Blake3Core.ChunkStart;
        if (isLastBlock) flags |= Blake3Core.ChunkEnd;

        // The root flag is applied on the final block only when no earlier chunks exist on the stack, meaning this is
        // the sole chunk and therefore the root. For multi-chunk inputs the root merge is deferred to
        // ProcessFinalBlock so that the root flag lands on the final parent compression.
        if (isFinal && _cvStackDepth == 0) flags |= Blake3Core.Root;

        Blake3Core.Compress(_chunkCv, block, chunkIndex, blockLen, flags);

        // Completed non-final chunks are pushed to the stack for pairwise tree merging.
        // PushSubtreeCv copies the CV into its merge buffer immediately, so the caller's _chunkCv may be reused for the
        // next chunk without cloning.
        if (isLastBlock && !isFinal)
            PushSubtreeCv(_chunkCv, 0, chunkIndex + 1);
    }

    /// <inheritdoc />
    /// <remarks>
    /// For single-chunk input the root chaining value is already in <see cref="_chunkCv" /> (with the root flag applied
    /// during <see cref="ProcessBlock" />). For multi-chunk input the CV stack is folded into the final chunk's
    /// chaining value via <see cref="MergeStackWithFinalChunk" />, which applies the root flag on the last parent
    /// compression.
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        Span<uint> rootCv = stackalloc uint[8];

        if (_cvStackDepth == 0)
            _chunkCv.AsSpan(0, 8).CopyTo(rootCv);
        else
            MergeStackWithFinalChunk(_chunkCv, rootCv);

        byte[] digest = new byte[OutLen];
        Blake3Core.StoreChainingValue(rootCv, digest);

        return digest;
    }

    /// <summary>
    /// Computes a parent node chaining value by compressing the concatenation of a left and a right child chaining
    /// value, writing the 8-word result into <paramref name="output" />.
    /// </summary>
    /// <param name="leftCv">The 8-word chaining value of the left child.</param>
    /// <param name="rightCv">The 8-word chaining value of the right child.</param>
    /// <param name="isRoot">
    /// <see langword="true" /> if this parent node is the root of the hash tree, causing the root flag to be applied.
    /// </param>
    /// <param name="output">
    /// Destination for the resulting 8-word parent chaining value. Must have a length of at least 8.
    /// </param>
    /// <remarks>
    /// Both inputs are encoded into the parent block before any write to <paramref name="output" />, so callers may
    /// safely alias <paramref name="rightCv" /> with <paramref name="output" /> to fold tree levels in place without an
    /// intermediate buffer. The block is cleared before the method returns.
    /// </remarks>
    private static void ParentCv(ReadOnlySpan<uint> leftCv, ReadOnlySpan<uint> rightCv, bool isRoot, Span<uint> output)
    {
        Span<byte> block = stackalloc byte[Blake3Core.BlockBytes];
        Blake3Core.StoreChainingValue(leftCv, block);
        Blake3Core.StoreChainingValue(rightCv, block[Blake3Core.ChainingValueBytes..]);

        // Parent nodes always use the key - the IV, for the unkeyed hash - as their chaining value input, counter 0.
        Blake3Core.InitializationVector.CopyTo(output);
        Blake3Core.Compress(output, block, 0UL, Blake3Core.BlockBytes, isRoot ? Blake3Core.Parent | Blake3Core.Root : Blake3Core.Parent);

        CryptographicOperations.ZeroMemory(block);
    }

    /// <summary>
    /// Merges the completed intermediate chunk stack with the final chunk chaining value and writes the resulting root
    /// chaining value into <paramref name="output" />.
    /// </summary>
    /// <param name="rightCv">
    /// The chaining value of the final chunk. This value is kept out of <see cref="_cvStack" /> until finalization so
    /// the last parent merge can be marked with the root flag.
    /// </param>
    /// <param name="output">Destination for the 8-word root chaining value. Must have a length of at least 8.</param>
    /// <remarks>
    /// <para>
    /// Intermediate chunks may already have been folded into balanced subtrees on <see cref="_cvStack" />. Finalization
    /// differs from normal chunk pushing because the final chunk must not be pre-merged as a non-root parent. Instead,
    /// the stack is folded into the final chunk from right to left, applying the root flag to the last parent
    /// compression.
    /// </para>
    /// </remarks>
    private void MergeStackWithFinalChunk(ReadOnlySpan<uint> rightCv, Span<uint> output)
    {
        Span<uint> working = stackalloc uint[8];
        rightCv[..8].CopyTo(working);

        while (_cvStackDepth > 0)
        {
            _cvStackDepth--;
            ReadOnlySpan<uint> leftCv = _cvStack.AsSpan(_cvStackDepth * 8, 8);

            bool isRoot = _cvStackDepth == 0;
            ParentCv(leftCv, working, isRoot, working);
        }

        working.CopyTo(output);
        working.Clear();
    }

    // ---- tree-merging stack helpers ----

    /// <summary>
    /// Hashes the whole chunks at the front of the input as complete subtrees, leaving at least one byte for the
    /// deferred final block.
    /// </summary>
    /// <param name="source">The input, starting on a chunk boundary with nothing buffered.</param>
    /// <returns>The number of bytes hashed: a whole number of chunks.</returns>
    /// <remarks>
    /// Each subtree is the largest run of whole chunks that leaves at least one byte behind, holds a power of two of
    /// chunks, and starts on a multiple of its own length, which makes it a node of the tree whatever input follows.
    /// The subtrees are hashed together, on up to <see cref="_maxDegreeOfParallelism" /> threads, and their chaining
    /// values join the stack in order, each as a chunk's would, one level up for every doubling of its size.
    /// </remarks>
    private int HashSubtrees(ReadOnlySpan<byte> source)
    {
        Span<int> subtreeChunks = stackalloc int[MaxSubtrees];
        ulong firstChunk = _totalBytes / ChunkSize;
        ulong chunkCount = firstChunk;
        int subtrees = 0;
        int consumed = 0;

        while (source.Length - consumed > ChunkSize && subtrees < MaxSubtrees)
        {
            int chunks = 1 << BitOperations.Log2((uint)((source.Length - consumed - 1) / ChunkSize));
            if (chunkCount != 0)
                chunks = (int)Math.Min((ulong)chunks, 1UL << BitOperations.TrailingZeroCount(chunkCount));

            subtreeChunks[subtrees++] = chunks;
            chunkCount += (ulong)chunks;
            consumed += chunks * ChunkSize;
        }

        Span<byte> chainingValues = stackalloc byte[MaxSubtrees * Blake3Core.ChainingValueBytes];
        Blake3Core.CompressSubtrees(source[..consumed], subtreeChunks[..subtrees], Blake3Core.InitializationVector, firstChunk, 0, _maxDegreeOfParallelism, chainingValues);

        Span<uint> chainingValue = stackalloc uint[8];
        chunkCount = firstChunk;
        for (int subtree = 0; subtree < subtrees; subtree++)
        {
            Blake3Core.LoadChainingValue(chainingValues[(subtree * Blake3Core.ChainingValueBytes)..], chainingValue);
            chunkCount += (ulong)subtreeChunks[subtree];
            PushSubtreeCv(chainingValue, BitOperations.Log2((uint)subtreeChunks[subtree]), chunkCount);
        }

        _totalBytes += (ulong)consumed;
        chainingValue.Clear();
        CryptographicOperations.ZeroMemory(chainingValues[..(subtrees * Blake3Core.ChainingValueBytes)]);

        return consumed;
    }

    /// <summary>
    /// Pushes the chaining value of a completed subtree onto <see cref="_cvStack" />, folding the top of the stack into
    /// the incoming CV whenever a balanced subtree boundary completes with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements the BLAKE3 push-chunk-chaining-value step from §2.1 of the specification, generalized to a subtree of
    /// <c>2^<paramref name="level" /></c> chunks - a single chunk at level 0. After it the total chunk count is
    /// <paramref name="chunkCount" />; the algorithm folds one tree level into the incoming CV for each trailing zero
    /// bit of that count above <paramref name="level" />. Each such bit indicates a balanced subtree of the
    /// corresponding height has just been completed, so the top stack entry (its left sibling) is popped and merged
    /// with the incoming CV via <see cref="ParentCv" />.
    /// </para>
    /// <para>
    /// After this step the live stack depth equals <c>popcount(<paramref name="chunkCount"/>)</c>, which is bounded at
    /// <see cref="MaxCvStackDepth" /> for any well-formed BLAKE3 input.
    /// </para>
    /// </remarks>
    /// <param name="cv">The 8-word chaining value of the completed subtree.</param>
    /// <param name="level">The base-2 logarithm of the subtree's chunk count.</param>
    /// <param name="chunkCount">The number of chunks hashed, the subtree's included.</param>
    /// <exception cref="InvalidOperationException">
    /// The stack already holds <see cref="MaxCvStackDepth" /> live levels. This is unreachable for any well-formed
    /// BLAKE3 input (which is bounded at <c>2^54</c> chunks) and indicates a corrupted streaming state.
    /// </exception>
    private void PushSubtreeCv(ReadOnlySpan<uint> cv, int level, ulong chunkCount)
    {
        // Fold the incoming CV into a stack-local working buffer so we can merge in place without allocating an array
        // per tree level.
        Span<uint> working = stackalloc uint[8];
        cv[..8].CopyTo(working);

        // BLAKE3 specifies merging one level for every trailing zero of the post-completion chunk count: each such bit
        // marks a balanced subtree boundary completing here. The subtree's own levels are already inside its CV.
        int mergeCount = BitOperations.TrailingZeroCount(chunkCount) - level;

        for (int i = 0; i < mergeCount; i++)
        {
            _cvStackDepth--;
            ReadOnlySpan<uint> left = _cvStack.AsSpan(_cvStackDepth * 8, 8);
            ParentCv(left, working, isRoot: false, working);
        }

        if (_cvStackDepth >= MaxCvStackDepth)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_Invalid_Blake3CvStackDepth, MaxCvStackDepth));
        }

        working.CopyTo(_cvStack.AsSpan(_cvStackDepth * 8, 8));
        _cvStackDepth++;
        working.Clear();
    }
}
