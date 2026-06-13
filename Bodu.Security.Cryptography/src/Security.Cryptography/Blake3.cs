// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Bodu.Extensions;

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
/// loop from <see cref="DeferredFinalBlockHashAlgorithm{T}" />. The final 64-byte block is not compressed until
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
/// <description>
/// Output size: 256 bits (32 bytes), fixed.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size: 64 bytes; chunk size: 1024 bytes (the leaf of the hash tree).
/// </description>
/// </item>
/// <item>
/// <description>
/// Construction: binary Merkle tree over chunks, ARX compression derived from BLAKE2 / ChaCha.
/// </description>
/// </item>
/// <item>
/// <description>
/// Mode: standard unkeyed hash only — keyed hash and KDF modes are not exposed.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose BLAKE3.</strong> Reach for BLAKE3 when raw throughput on long inputs is the priority — its
/// tree structure is naturally parallel-friendly and outperforms <see cref="Blake2b" />, SHA-2, and SHA-3 on
/// multi-megabyte messages. For short inputs the difference shrinks and any of the BLAKE2 / SHA-2 variants is fine. Use
/// <see cref="Blake2b" /> if a configurable output size or RFC 7693-compatible MAC mode is required; use
/// <see cref="MerkleTreeHash" /> or <see cref="ParallelMerkleTreeHash" /> if you want explicit control over the tree
/// shape and the underlying leaf hash.
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
public sealed partial class Blake3
    : DeferredFinalBlockHashAlgorithm<Blake3>
{
    /// <summary>
    /// Size, in bytes, of a single compression input block.
    /// </summary>
    private new const int BlockSize = 64;

    /// <summary>
    /// Size, in bytes, of a single input chunk (leaf of the hash tree).
    /// </summary>
    private const int ChunkSize = 1024;

    /// <summary>
    /// Maximum possible depth of the chaining-value stack. BLAKE3 supports up to <c>2^54</c> chunks per message, so the
    /// Merkle tree height is bounded at 54 — any well-formed input fits within this bound.
    /// </summary>
    private const int MaxCvStackDepth = 54;

    /// <summary>
    /// The flag applied to the last compression block of every chunk.
    /// </summary>
    private const uint FlagChunkEnd = 2u;
    // ---- domain-separation flags (§2.5 of the BLAKE3 specification) ----

    /// <summary>
    /// The flag applied to the first compression block of every chunk.
    /// </summary>
    private const uint FlagChunkStart = 1u;

    /// <summary>
    /// The flag applied to every parent (non-leaf) node compression call.
    /// </summary>
    private const uint FlagParent = 4u;

    /// <summary>
    /// The flag applied to the root compression call to enable output extraction.
    /// </summary>
    private const uint FlagRoot = 8u;

    /// <summary>
    /// Output length in bytes.
    /// </summary>
    private const int OutLen = 32;

    /// <summary>
    /// The BLAKE3 initialization vector, taken from the fractional parts of the square roots of the first eight prime
    /// numbers, identical to the SHA-256 IV.
    /// </summary>
    private static readonly uint[] s_iv =
    [
        0x6A09E667u, 0xBB67AE85u, 0x3C6EF372u, 0xA54FF53Au,
        0x510E527Fu, 0x9B05688Cu, 0x1F83D9ABu, 0x5BE0CD19u,
    ];

    /// <summary>
    /// The per-round message word permutation table (§2.4 of the BLAKE3 specification). Each row gives the 16
    /// message-word indices consumed by a single round's G calls.
    /// </summary>
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
    private static readonly byte[,] s_msgSchedule =
    {
        {  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 },
        {  2,  6,  3, 10,  7,  0,  4, 13,  1, 11, 12,  5,  9, 14, 15,  8 },
        {  3,  4, 10, 12, 13,  2,  7, 14,  6,  5,  9,  0, 11, 15,  8,  1 },
        { 10,  7, 12,  9, 14,  3, 13, 15,  4,  0, 11,  2,  5,  8,  1,  6 },
        { 12, 13,  9, 11, 15, 10, 14,  8,  7,  2,  5,  3,  0,  1,  6,  4 },
        {  9, 14, 11,  5,  8, 12, 15,  1, 13,  3,  0, 10,  2,  6,  4,  7 },
        { 11, 15,  5,  0,  1,  9,  8,  6, 14, 10,  2, 12,  3,  4,  7, 13 },
    };
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row

    /// <summary>
    /// Running chaining value for the chunk currently being compressed.
    /// </summary>
    /// <remarks>
    /// Reset to the IV at the start of each new chunk (when the first block of a chunk is processed) and updated in
    /// place after every compression call. Carries the accumulated chaining state block-by-block until the chunk
    /// completes.
    /// </remarks>
    private readonly uint[] _chunkCv = new uint[8];

    /// <summary>
    /// Chaining-value stack used to build parent nodes as chunks complete. Laid out as a flat 8-word slice per level,
    /// indexed by <see cref="_cvStackDepth" />, so per-level pushes and merges run without per-level array allocations.
    /// </summary>
    private readonly uint[] _cvStack = new uint[MaxCvStackDepth * 8];

    /// <summary>
    /// Pre-allocated 16-word scratch buffer used by <see cref="ParentCv" /> to assemble the concatenation of a left and
    /// a right child chaining value before invoking <see cref="Compress" />. Reusing this field removes the per-call
    /// <c>uint[16]</c> allocation that would otherwise occur at every tree-merge step.
    /// </summary>
    private readonly uint[] _parentBlockWords = new uint[16];

    /// <summary>
    /// Current depth of <see cref="_cvStack" /> — the number of 8-word CV slices currently live, with the active top
    /// slice occupying words <c>[(_cvStackDepth - 1) * 8, _cvStackDepth * 8)</c> when non-zero.
    /// </summary>
    private int _cvStackDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake3" /> class, configured to produce a 256-bit digest.
    /// </summary>
    public Blake3()
        : base(BlockSize * 8)
    {
        HashSizeValue = 256;
        s_iv.CopyTo(_chunkCv, 0);
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
    /// Gets a value indicating whether this transform instance can be reused after a hash operation is completed.
    /// </summary>
    /// <returns>
    /// <see langword="true" />; <see cref="Blake3" /> resets its state automatically and may be reused across multiple
    /// <c>ComputeHash</c> calls.
    /// </returns>
    public override bool CanReuseTransform => true;

    /// <summary>
    /// Gets a value indicating whether multiple blocks may be transformed in a single
    /// <see cref="HashAlgorithm.TransformBlock" /> call.
    /// </summary>
    /// <returns><see langword="true" />; the implementation accumulates arbitrary-length input internally.</returns>
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
        s_iv.CopyTo(_chunkCv, 0);
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

            CryptographyHelper.Clear(_parentBlockWords);
            CryptographyHelper.Clear(_chunkCv);
        }

        base.Dispose(disposing);
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
        var adjustedTotal = totalBytesIncludingThisBlock == 0 ? 0UL : totalBytesIncludingThisBlock - 1;
        var chunkIndex = adjustedTotal / (ulong)ChunkSize;
        var isFirstBlock = (adjustedTotal % (ulong)ChunkSize) / (ulong)BlockSize == 0;
        var isLastBlock = totalBytesIncludingThisBlock % (ulong)ChunkSize == 0 || isFinal;

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
            var rem = totalBytesIncludingThisBlock % (ulong)BlockSize;
            blockLen = (uint)(rem == 0 ? BlockSize : (int)rem);
        }

        // Each new chunk begins from the IV.
        if (isFirstBlock)
            s_iv.CopyTo(_chunkCv, 0);

        var flags = 0u;
        if (isFirstBlock) flags |= FlagChunkStart;
        if (isLastBlock) flags |= FlagChunkEnd;

        // FlagRoot is applied on the final block only when no earlier chunks exist on the stack,
        // meaning this is the sole chunk and therefore the root.  For multi-chunk inputs the root
        // merge is deferred to ProcessFinalBlock so that FlagRoot lands on the final parent compression.
        if (isFinal && _cvStackDepth == 0) flags |= FlagRoot;

        var blockWords = ReadBlockWords(block);
        var state = Compress(_chunkCv, blockWords, chunkIndex, blockLen, flags);

        for (var i = 0; i < 8; i++)
            _chunkCv[i] = state[i];

        // Completed non-final chunks are pushed to the stack for pairwise tree merging.
        // PushChunkCv copies the CV into its merge buffer immediately, so the caller's _chunkCv may be reused for the
        // next chunk without cloning.
        if (isLastBlock && !isFinal)
            PushChunkCv(_chunkCv, chunkIndex);
    }

    /// <inheritdoc />
    /// <remarks>
    /// For single-chunk input the root chaining value is already in <see cref="_chunkCv" /> (with
    /// <see cref="FlagRoot" /> applied during <see cref="ProcessBlock" />). For multi-chunk input the CV stack is
    /// folded into the final chunk's chaining value via <see cref="MergeStackWithFinalChunk" />, which applies
    /// <see cref="FlagRoot" /> on the last parent compression.
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        Span<uint> rootCv = stackalloc uint[8];

        if (_cvStackDepth == 0)
            _chunkCv.AsSpan(0, 8).CopyTo(rootCv);
        else
            MergeStackWithFinalChunk(_chunkCv, rootCv);

        var digest = new byte[OutLen];

        for (var i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(i * 4), rootCv[i]);

        return digest;
    }

    /// <summary>
    /// Performs the BLAKE3 compression function, transforming a chaining value and a 16-word message block into a
    /// 16-word output state.
    /// </summary>
    /// <param name="cv">The 8-word input chaining value. Must have a length of at least 8.</param>
    /// <param name="blockWords">
    /// The 16-word message block in little-endian order. Must have a length of at least 16.
    /// </param>
    /// <param name="counter">The 64-bit chunk counter encoding the position of this chunk in the input stream.</param>
    /// <param name="blockLen">The number of input bytes represented by <paramref name="blockWords" /> (0–64).</param>
    /// <param name="flags">
    /// The bitwise OR of any applicable domain-separation flags (<see cref="FlagChunkStart" />,
    /// <see cref="FlagChunkEnd" />, <see cref="FlagParent" />, <see cref="FlagRoot" />).
    /// </param>
    /// <returns>
    /// A 16-element array containing the post-compression state, whose first eight words form the updated chaining
    /// value.
    /// </returns>
    /// <remarks>
    /// Dispatches to an AVX-512 vectorised implementation when supported by the host, falling back to a scalar
    /// reference implementation otherwise. <see cref="Avx512F.VL.IsSupported" /> is a JIT intrinsic that folds to a
    /// compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint[] Compress(uint[] cv, uint[] blockWords, ulong counter, uint blockLen, uint flags) =>
         Avx512F.VL.IsSupported
            ? CompressAvx512(cv, blockWords, counter, blockLen, flags)
            : CompressScalar(cv, blockWords, counter, blockLen, flags);

    /// <summary>
    /// Scalar reference implementation of the BLAKE3 compression function used as a fallback on hosts without AVX-512 +
    /// VL support. Implements §2.4 of the BLAKE3 specification directly on a 16-element <see cref="uint" /> array.
    /// </summary>
    /// <param name="cv">The 8-word input chaining value.</param>
    /// <param name="blockWords">The 16-word message block.</param>
    /// <param name="counter">The 64-bit chunk counter.</param>
    /// <param name="blockLen">The number of input bytes in the block.</param>
    /// <param name="flags">The BLAKE3 domain-separation flags for the block.</param>
    /// <returns>The 16-word output state produced by the compression function.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static uint[] CompressScalar(uint[] cv, uint[] blockWords, ulong counter, uint blockLen, uint flags)
    {
        var state = new uint[16];

        // Upper half: chaining value.
        state[0] = cv[0];
        state[1] = cv[1];
        state[2] = cv[2];
        state[3] = cv[3];
        state[4] = cv[4];
        state[5] = cv[5];
        state[6] = cv[6];
        state[7] = cv[7];

        // Lower half: IV words, counter split into two 32-bit halves, block length, flags.
        state[8] = s_iv[0];
        state[9] = s_iv[1];
        state[10] = s_iv[2];
        state[11] = s_iv[3];
        state[12] = (uint)counter;
        state[13] = (uint)(counter >> 32);
        state[14] = blockLen;
        state[15] = flags;

        // Seven rounds of column then diagonal mixing, each using a permuted view of the message block.
        for (var round = 0; round < 7; round++)
        {
            // Column step.
            G(state, 0, 4, 8, 12, blockWords[s_msgSchedule[round, 0]], blockWords[s_msgSchedule[round, 1]]);
            G(state, 1, 5, 9, 13, blockWords[s_msgSchedule[round, 2]], blockWords[s_msgSchedule[round, 3]]);
            G(state, 2, 6, 10, 14, blockWords[s_msgSchedule[round, 4]], blockWords[s_msgSchedule[round, 5]]);
            G(state, 3, 7, 11, 15, blockWords[s_msgSchedule[round, 6]], blockWords[s_msgSchedule[round, 7]]);

            // Diagonal step.
            G(state, 0, 5, 10, 15, blockWords[s_msgSchedule[round, 8]], blockWords[s_msgSchedule[round, 9]]);
            G(state, 1, 6, 11, 12, blockWords[s_msgSchedule[round, 10]], blockWords[s_msgSchedule[round, 11]]);
            G(state, 2, 7, 8, 13, blockWords[s_msgSchedule[round, 12]], blockWords[s_msgSchedule[round, 13]]);
            G(state, 3, 4, 9, 14, blockWords[s_msgSchedule[round, 14]], blockWords[s_msgSchedule[round, 15]]);
        }

        // Finalize: XOR the two halves of the state.
        for (var i = 0; i < 8; i++)
            state[i] ^= state[i + 8];

        // Mix the original chaining value into the high half.
        for (var i = 8; i < 16; i++)
            state[i] ^= cv[i - 8];

        return state;
    }

    /// <summary>
    /// Applies the BLAKE3 quarter-round mixing function <c>G</c> to four positions in the working state, using two
    /// message words <paramref name="mx" /> and <paramref name="my" />.
    /// </summary>
    /// <param name="state">The 16-word working state array, modified in place.</param>
    /// <param name="a">Index of the first state word.</param>
    /// <param name="b">Index of the second state word.</param>
    /// <param name="c">Index of the third state word.</param>
    /// <param name="d">Index of the fourth state word.</param>
    /// <param name="mx">First message word for this mixing step.</param>
    /// <param name="my">Second message word for this mixing step.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(uint[] state, int a, int b, int c, int d, uint mx, uint my)
    {
        state[a] = state[a] + state[b] + mx;
        state[d] = (state[d] ^ state[a]).RotateBitsRightUnchecked(16);
        state[c] = state[c] + state[d];
        state[b] = (state[b] ^ state[c]).RotateBitsRightUnchecked(12);
        state[a] = state[a] + state[b] + my;
        state[d] = (state[d] ^ state[a]).RotateBitsRightUnchecked(8);
        state[c] = state[c] + state[d];
        state[b] = (state[b] ^ state[c]).RotateBitsRightUnchecked(7);
    }

    /// <summary>
    /// Computes a parent node chaining value by compressing the concatenation of a left and a right child chaining
    /// value, writing the 8-word result into <paramref name="output" />.
    /// </summary>
    /// <param name="leftCv">The 8-word chaining value of the left child.</param>
    /// <param name="rightCv">The 8-word chaining value of the right child.</param>
    /// <param name="isRoot">
    /// <see langword="true" /> if this parent node is the root of the hash tree, causing <see cref="FlagRoot" /> to be
    /// applied.
    /// </param>
    /// <param name="output">
    /// Destination for the resulting 8-word parent chaining value. Must have a length of at least 8.
    /// </param>
    /// <remarks>
    /// Both inputs are copied into <see cref="_parentBlockWords" /> before any write to <paramref name="output" />, so
    /// callers may safely alias <paramref name="rightCv" /> with <paramref name="output" /> to fold tree levels in
    /// place without an intermediate buffer.
    /// </remarks>
    private void ParentCv(ReadOnlySpan<uint> leftCv, ReadOnlySpan<uint> rightCv, bool isRoot, Span<uint> output)
    {
        // Materialise both children into the scratch field BEFORE writing the output span. This is what makes the
        // (rightCv, output) aliasing in PushChunkCv / MergeStackWithFinalChunk safe.
        leftCv.CopyTo(_parentBlockWords.AsSpan(0, 8));
        rightCv.CopyTo(_parentBlockWords.AsSpan(8, 8));

        var flags = FlagParent;

        if (isRoot)
            flags |= FlagRoot;

        // Parent nodes always use the IV as their chaining value input, counter = 0.
        var outState = Compress(s_iv, _parentBlockWords, 0UL, BlockSize, flags);

        outState.AsSpan(0, 8).CopyTo(output);
    }

    /// <summary>
    /// Reads exactly 64 bytes from <paramref name="block" /> into a 16-element little-endian uint32 word array,
    /// zero-padding any bytes beyond the actual block length.
    /// </summary>
    /// <param name="block">The raw block bytes to interpret (0–64 bytes).</param>
    /// <returns>A 16-element array of little-endian uint32 words representing the block.</returns>
    private static uint[] ReadBlockWords(ReadOnlySpan<byte> block)
    {
        Span<byte> padded = stackalloc byte[BlockSize];
        padded.Clear();
        block.CopyTo(padded);

        var words = new uint[16];

        for (var i = 0; i < 16; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(padded[(i * 4)..]);

        return words;
    }

    /// <summary>
    /// Merges the completed intermediate chunk stack with the final chunk chaining value and writes the resulting root
    /// chaining value into <paramref name="output" />.
    /// </summary>
    /// <param name="rightCv">
    /// The chaining value of the final chunk. This value is kept out of <see cref="_cvStack" /> until finalization so
    /// the last parent merge can be marked with <see cref="FlagRoot" />.
    /// </param>
    /// <param name="output">Destination for the 8-word root chaining value. Must have a length of at least 8.</param>
    /// <remarks>
    /// <para>
    /// Intermediate chunks may already have been folded into balanced subtrees on <see cref="_cvStack" />. Finalization
    /// differs from normal chunk pushing because the final chunk must not be pre-merged as a non-root parent. Instead,
    /// the stack is folded into the final chunk from right to left, applying <see cref="FlagRoot" /> to the last parent
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

            var isRoot = _cvStackDepth == 0;
            ParentCv(leftCv, working, isRoot, working);
        }

        working.CopyTo(output);
    }

    // ---- tree-merging stack helpers ----

    /// <summary>
    /// Pushes a completed chunk chaining value onto <see cref="_cvStack" />, folding the top of the stack into the
    /// incoming CV whenever a balanced subtree boundary completes at this chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements the BLAKE3 push-chunk-chaining-value step from §2.1 of the specification. After completing the chunk
    /// at zero-based index <paramref name="chunkIdx" /> the total chunk count is
    /// <c>(<paramref name="chunkIdx" /> + 1)</c>; the algorithm folds one tree level into the incoming CV for each
    /// trailing zero bit of that count. Each trailing zero indicates a balanced subtree of the corresponding height has
    /// just been completed, so the top stack entry (its left sibling) is popped and merged with the incoming CV via
    /// <see cref="ParentCv" />.
    /// </para>
    /// <para>
    /// After this step the live stack depth equals <c>popcount(<paramref name="chunkIdx"/> + 1)</c>, which is bounded
    /// at <see cref="MaxCvStackDepth" /> for any well-formed BLAKE3 input.
    /// </para>
    /// </remarks>
    /// <param name="cv">The 8-word chaining value produced by the completed chunk.</param>
    /// <param name="chunkIdx">The zero-based chunk index of the completed chunk.</param>
    /// <exception cref="InvalidOperationException">
    /// The stack already holds <see cref="MaxCvStackDepth" /> live levels. This is unreachable for any well-formed
    /// BLAKE3 input (which is bounded at <c>2^54</c> chunks) and indicates a corrupted streaming state.
    /// </exception>
    private void PushChunkCv(ReadOnlySpan<uint> cv, ulong chunkIdx)
    {
        // Fold the incoming CV into a stack-local working buffer so we can merge in place without allocating an array
        // per tree level.
        Span<uint> working = stackalloc uint[8];
        cv[..8].CopyTo(working);

        // BLAKE3 specifies merging one level for every trailing zero of the post-completion chunk count: each such bit
        // marks a balanced subtree boundary completing at this chunk.
        var completed = chunkIdx + 1;
        var mergeCount = System.Numerics.BitOperations.TrailingZeroCount(completed);

        for (var i = 0; i < mergeCount; i++)
        {
            _cvStackDepth--;
            ReadOnlySpan<uint> left = _cvStack.AsSpan(_cvStackDepth * 8, 8);
            ParentCv(left, working, isRoot: false, working);
        }

        if (_cvStackDepth >= MaxCvStackDepth)
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_Invalid_Blake3CvStackDepth, MaxCvStackDepth));

        working.CopyTo(_cvStack.AsSpan(_cvStackDepth * 8, 8));
        _cvStackDepth++;
    }
}
