// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 256-bit cryptographic hash using the <c>BLAKE3</c> algorithm designed by Jack
/// O'Connor, Jean-Philippe Aumasson, Samuel Neves, and Zooko Wilcox-O'Hearn. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE3 is a cryptographic hash function that combines the speed of non-cryptographic hashes
/// with strong security guarantees. It is based on a binary tree structure where each leaf
/// (chunk) processes up to 1024 bytes of input and each internal (parent) node combines two
/// child chaining values. All compression is performed by a single ARX-based function derived
/// from the BLAKE2 and ChaCha families.
/// </para>
/// <para>
/// Input is divided into 1024-byte chunks, each compressed block-by-block into an 8-word
/// (256-bit) chaining value. When more than one chunk exists the chaining values are folded
/// pairwise into parent nodes until a single root chaining value remains. The root compression
/// call is distinguished by the <c>ROOT</c> domain-separation flag, which enables XOF-style
/// output extraction; this implementation fixes the output length at 256 bits.
/// </para>
/// <para>
/// This implementation inherits its 64-byte residual buffer, running byte counter, and
/// defer-on-full-block buffering loop from <see cref="DeferredFinalBlockHashAlgorithm{T}" />.
/// The final 64-byte block is not compressed until <see cref="HashAlgorithm.HashFinal" /> is
/// called, ensuring that chunk-level and tree-level domain flags can be applied correctly.
/// </para>
/// <para>
/// This implementation supports the standard, unkeyed hash mode only. Keyed-hash and
/// key-derivation modes are not exposed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var blake3 = new Blake3();
/// byte[] digest = blake3.ComputeHash(message);
/// </code>
/// </example>
public sealed class Blake3
    : DeferredFinalBlockHashAlgorithm<Blake3>
{
    // ---- domain-separation flags (§2.5 of the BLAKE3 specification) ----

    /// <summary>The flag applied to the first compression block of every chunk.</summary>
    private const uint FlagChunkStart = 1u;

    /// <summary>The flag applied to the last compression block of every chunk.</summary>
    private const uint FlagChunkEnd = 2u;

    /// <summary>The flag applied to every parent (non-leaf) node compression call.</summary>
    private const uint FlagParent = 4u;

    /// <summary>The flag applied to the root compression call to enable output extraction.</summary>
    private const uint FlagRoot = 8u;

    // ---- structural constants ----

    /// <summary>Size, in bytes, of a single compression input block.</summary>
    private const int BlockSize = 64;

    /// <summary>Size, in bytes, of a single input chunk (leaf of the hash tree).</summary>
    private const int ChunkSize = 1024;

    /// <summary>Output length in bytes.</summary>
    private const int OutLen = 32;

    // ---- initialisation vector (first eight words of the SHA-256 IV) ----

    /// <summary>
    /// The BLAKE3 initialisation vector, taken from the fractional parts of the square roots of
    /// the first eight prime numbers, identical to the SHA-256 IV.
    /// </summary>
    private static readonly uint[] s_iv =
    {
        0x6A09E667u, 0xBB67AE85u, 0x3C6EF372u, 0xA54FF53Au,
        0x510E527Fu, 0x9B05688Cu, 0x1F83D9ABu, 0x5BE0CD19u,
    };

    /// <summary>
    /// The per-round message word permutation table (§2.4 of the BLAKE3 specification).
    /// Each row gives the 16 message-word indices consumed by a single round's G calls.
    /// </summary>
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

    // ---- streaming state ----

    /// <summary>Running chaining value for the chunk currently being compressed.</summary>
    /// <remarks>
    /// Reset to the IV at the start of each new chunk (when the first block of a chunk is
    /// processed) and updated in place after every compression call. Carries the accumulated
    /// chaining state block-by-block until the chunk completes.
    /// </remarks>
    private readonly uint[] _chunkCv = new uint[8];

    /// <summary>Chaining-value stack used to build parent nodes as chunks complete.</summary>
    private readonly List<uint[]> _cvStack = new();

    // ---- construction ----

    /// <summary>
    /// Initialises a new instance of the <see cref="Blake3" /> class, configured to produce a
    /// 256-bit digest.
    /// </summary>
    public Blake3()
        : base(BlockSize)
    {
        HashSizeValue = 256;
        s_iv.CopyTo(_chunkCv, 0);
    }

    // ---- HashAlgorithm overrides ----

    /// <summary>
    /// Gets a value indicating whether this transform instance can be reused after a hash
    /// operation is completed.
    /// </summary>
    /// <returns>
    /// <see langword="true" />; <see cref="Blake3" /> resets its state automatically and may be
    /// reused across multiple <c>ComputeHash</c> calls.
    /// </returns>
    public override bool CanReuseTransform => true;

    /// <summary>
    /// Gets a value indicating whether multiple blocks may be transformed in a single
    /// <see cref="HashAlgorithm.TransformBlock" /> call.
    /// </summary>
    /// <returns>
    /// <see langword="true" />; the implementation accumulates arbitrary-length input internally.
    /// </returns>
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets the preferred input block size, in bytes, used when feeding data through
    /// <see cref="System.Security.Cryptography.CryptoStream" />.
    /// </summary>
    /// <returns>
    /// <see cref="BlockSize" /> (64 bytes) — one BLAKE3 compression block.
    /// </returns>
    public override int InputBlockSize => BlockSize;

    /// <summary>
    /// Gets the output block size, in bytes.
    /// </summary>
    /// <returns>
    /// <see cref="OutLen" /> (32 bytes), the fixed 256-bit digest length produced by this
    /// implementation.
    /// </returns>
    public override int OutputBlockSize => OutLen;

    /// <inheritdoc />
    public override void Initialize()
    {
        ThrowIfDisposed();
        base.Initialize();
    }

    // ---- DeferredFinalBlockHashAlgorithm<T> implementation ----

    /// <inheritdoc />
    /// <remarks>
    /// Clears the CV stack and restores <see cref="_chunkCv" /> to the BLAKE3 initialisation
    /// vector, ready for a new chunk.
    /// </remarks>
    protected override void OnInitialize()
    {
        _cvStack.Clear();
        s_iv.CopyTo(_chunkCv, 0);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Clears the CV stack, zeroes <see cref="_chunkCv" />, releases the framework
    /// <see cref="HashAlgorithm.HashValue" /> array, and zeroes
    /// <see cref="HashAlgorithm.HashSizeValue" />. The inherited residual buffer is cleared by
    /// the grandparent before this hook runs.
    /// </remarks>
    protected override void OnDispose(bool disposing)
    {
        if (disposing)
        {
            _cvStack.Clear();
            Array.Clear(_chunkCv, 0, _chunkCv.Length);
            CryptoHelpers.ClearAndNullify(ref HashValue);
            HashSizeValue = 0;
        }
    }

    /// <summary>
    /// Advances the BLAKE3 compression state by one 64-byte block, applying the correct
    /// chunk-level and tree-level domain flags derived from <paramref name="totalBytesIncludingThisBlock" />.
    /// </summary>
    /// <param name="block">
    /// The 64-byte block to compress. Zero-padded by the base class when <paramref name="isFinal" />
    /// is <see langword="true" /> and the final message byte count is not a multiple of 64.
    /// </param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count including the bytes in this block. Used to derive the chunk
    /// index, the block position within the chunk, and the true block length for the final block.
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> for the last compression call, raised by
    /// <see cref="HashAlgorithm.HashFinal" />; otherwise <see langword="false" />.
    /// </param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Derive chunk position. Subtracting 1 maps [1, 64] → block 0 of chunk 0, etc.
        // The zero guard handles the empty-input case where totalBytes is 0.
        ulong adjustedTotal = totalBytesIncludingThisBlock == 0 ? 0UL : totalBytesIncludingThisBlock - 1;
        ulong chunkIndex    = adjustedTotal / (ulong)ChunkSize;
        bool isFirstBlock   = adjustedTotal % (ulong)ChunkSize / (ulong)BlockSize == 0;
        bool isLastBlock    = totalBytesIncludingThisBlock % (ulong)ChunkSize == 0 || isFinal;

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
            s_iv.CopyTo(_chunkCv, 0);

        uint flags = 0u;
        if (isFirstBlock)                    flags |= FlagChunkStart;
        if (isLastBlock)                     flags |= FlagChunkEnd;
        // FlagRoot is applied on the final block only when no earlier chunks exist on the stack,
        // meaning this is the sole chunk and therefore the root.  For multi-chunk inputs the root
        // merge is deferred to ProcessFinalBlock so that FlagRoot lands on the final parent compression.
        if (isFinal && _cvStack.Count == 0)  flags |= FlagRoot;

        uint[] blockWords = ReadBlockWords(block);
        uint[] state      = Compress(_chunkCv, blockWords, chunkIndex, blockLen, flags);

        for (int i = 0; i < 8; i++)
            _chunkCv[i] = state[i];

        // Completed non-final chunks are pushed to the stack for pairwise tree merging.
        if (isLastBlock && !isFinal)
            PushChunkCv((uint[])_chunkCv.Clone(), chunkIndex);
    }

    /// <inheritdoc />
    /// <remarks>
    /// For single-chunk input the root chaining value is already in <see cref="_chunkCv" />
    /// (with <see cref="FlagRoot" /> applied during <see cref="ProcessBlock" />). For
    /// multi-chunk input the CV stack is folded into the final chunk's chaining value via
    /// <see cref="MergeStackWithFinalChunk" />, which applies <see cref="FlagRoot" /> on the
    /// last parent compression.
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        uint[] rootCv = _cvStack.Count == 0
            ? _chunkCv
            : MergeStackWithFinalChunk(_chunkCv);

        byte[] digest = new byte[OutLen];

        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(i * 4), rootCv[i]);

        return digest;
    }

    /// <summary>
    /// Merges the completed intermediate chunk stack with the final chunk chaining value and
    /// returns the root chaining value.
    /// </summary>
    /// <param name="rightCv">
    /// The chaining value of the final chunk. This value is kept out of <see cref="_cvStack" />
    /// until finalisation so the last parent merge can be marked with <see cref="FlagRoot" />.
    /// </param>
    /// <returns>
    /// An 8-element array containing the 256-bit root chaining value of the complete message.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Intermediate chunks may already have been folded into balanced subtrees on
    /// <see cref="_cvStack" />. Finalisation differs from normal chunk pushing because the final
    /// chunk must not be pre-merged as a non-root parent. Instead, the stack is folded into the
    /// final chunk from right to left, applying <see cref="FlagRoot" /> to the last parent
    /// compression.
    /// </para>
    /// </remarks>
    private uint[] MergeStackWithFinalChunk(uint[] rightCv)
    {
        uint[] cv = rightCv;

        while (_cvStack.Count > 0)
        {
            int lastIdx = _cvStack.Count - 1;

            uint[] leftCv = _cvStack[lastIdx];
            _cvStack.RemoveAt(lastIdx);

            bool isRoot = _cvStack.Count == 0;
            cv = ParentCv(leftCv, cv, isRoot);
        }

        return cv;
    }

    // ---- core compression ----

    /// <summary>
    /// Performs the BLAKE3 compression function, transforming a chaining value and a 16-word
    /// message block into a 16-word output state.
    /// </summary>
    /// <param name="cv">
    /// The 8-word input chaining value. Must have a length of at least 8.
    /// </param>
    /// <param name="blockWords">
    /// The 16-word message block in little-endian order. Must have a length of at least 16.
    /// </param>
    /// <param name="counter">
    /// The 64-bit chunk counter encoding the position of this chunk in the input stream.
    /// </param>
    /// <param name="blockLen">
    /// The number of input bytes represented by <paramref name="blockWords" /> (0–64).
    /// </param>
    /// <param name="flags">
    /// The bitwise OR of any applicable domain-separation flags (<see cref="FlagChunkStart" />,
    /// <see cref="FlagChunkEnd" />, <see cref="FlagParent" />, <see cref="FlagRoot" />).
    /// </param>
    /// <returns>
    /// A 16-element array containing the post-compression state, whose first eight words form
    /// the updated chaining value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint[] Compress(uint[] cv, uint[] blockWords, ulong counter, uint blockLen, uint flags)
    {
        uint[] state = new uint[16];

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
        state[8]  = s_iv[0];
        state[9]  = s_iv[1];
        state[10] = s_iv[2];
        state[11] = s_iv[3];
        state[12] = (uint)counter;
        state[13] = (uint)(counter >> 32);
        state[14] = blockLen;
        state[15] = flags;

        // Seven rounds of column then diagonal mixing, each using a permuted view of the message block.
        for (int round = 0; round < 7; round++)
        {
            // Column step.
            G(state, 0, 4,  8, 12, blockWords[s_msgSchedule[round,  0]], blockWords[s_msgSchedule[round,  1]]);
            G(state, 1, 5,  9, 13, blockWords[s_msgSchedule[round,  2]], blockWords[s_msgSchedule[round,  3]]);
            G(state, 2, 6, 10, 14, blockWords[s_msgSchedule[round,  4]], blockWords[s_msgSchedule[round,  5]]);
            G(state, 3, 7, 11, 15, blockWords[s_msgSchedule[round,  6]], blockWords[s_msgSchedule[round,  7]]);

            // Diagonal step.
            G(state, 0, 5, 10, 15, blockWords[s_msgSchedule[round,  8]], blockWords[s_msgSchedule[round,  9]]);
            G(state, 1, 6, 11, 12, blockWords[s_msgSchedule[round, 10]], blockWords[s_msgSchedule[round, 11]]);
            G(state, 2, 7,  8, 13, blockWords[s_msgSchedule[round, 12]], blockWords[s_msgSchedule[round, 13]]);
            G(state, 3, 4,  9, 14, blockWords[s_msgSchedule[round, 14]], blockWords[s_msgSchedule[round, 15]]);
        }

        // Finalise: XOR the two halves of the state.
        for (int i = 0; i < 8; i++)
            state[i] ^= state[i + 8];

        // Mix the original chaining value into the high half.
        for (int i = 8; i < 16; i++)
            state[i] ^= cv[i - 8];

        return state;
    }

    /// <summary>
    /// Applies the BLAKE3 quarter-round mixing function <c>G</c> to four positions in the
    /// working state, using two message words <paramref name="mx" /> and <paramref name="my" />.
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
        state[d] = RotateRight(state[d] ^ state[a], 16);
        state[c] = state[c] + state[d];
        state[b] = RotateRight(state[b] ^ state[c], 12);
        state[a] = state[a] + state[b] + my;
        state[d] = RotateRight(state[d] ^ state[a], 8);
        state[c] = state[c] + state[d];
        state[b] = RotateRight(state[b] ^ state[c], 7);
    }

    /// <summary>
    /// Rotates <paramref name="value" /> right by <paramref name="bits" /> positions.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The number of bit positions to rotate right.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits) =>
        (value >> bits) | (value << (32 - bits));

    // ---- block and parent processing ----

    /// <summary>
    /// Reads exactly 64 bytes from <paramref name="block" /> into a 16-element little-endian
    /// uint32 word array, zero-padding any bytes beyond the actual block length.
    /// </summary>
    /// <param name="block">The raw block bytes to interpret (0–64 bytes).</param>
    /// <returns>A 16-element array of little-endian uint32 words representing the block.</returns>
    private static uint[] ReadBlockWords(ReadOnlySpan<byte> block)
    {
        Span<byte> padded = stackalloc byte[BlockSize];
        padded.Clear();
        block.CopyTo(padded);

        uint[] words = new uint[16];

        for (int i = 0; i < 16; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(padded.Slice(i * 4));

        return words;
    }

    /// <summary>
    /// Computes a parent node chaining value by compressing the concatenation of a left and a
    /// right child chaining value.
    /// </summary>
    /// <param name="leftCv">The 8-word chaining value of the left child.</param>
    /// <param name="rightCv">The 8-word chaining value of the right child.</param>
    /// <param name="isRoot">
    /// <see langword="true" /> if this parent node is the root of the hash tree, causing
    /// <see cref="FlagRoot" /> to be applied.
    /// </param>
    /// <returns>
    /// An 8-element array containing the parent node's 256-bit chaining value.
    /// </returns>
    private static uint[] ParentCv(uint[] leftCv, uint[] rightCv, bool isRoot)
    {
        // The block words for a parent node are the concatenation of the two child CVs.
        uint[] blockWords = new uint[16];
        leftCv.AsSpan(0, 8).CopyTo(blockWords.AsSpan(0));
        rightCv.AsSpan(0, 8).CopyTo(blockWords.AsSpan(8));

        uint flags = FlagParent;

        if (isRoot)
            flags |= FlagRoot;

        // Parent nodes always use the IV as their chaining value input, counter = 0.
        uint[] outState = Compress(s_iv, blockWords, 0UL, BlockSize, flags);

        return new uint[] { outState[0], outState[1], outState[2], outState[3], outState[4], outState[5], outState[6], outState[7] };
    }

    // ---- tree-merging stack helpers ----

    /// <summary>
    /// Pushes a completed chunk chaining value onto <see cref="_cvStack" />, merging adjacent
    /// pairs into parent nodes whenever the binary tree structure requires it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BLAKE3 uses a Merkle tree with a power-of-two fan-out. The merge condition is: after
    /// completing chunk at zero-based index <paramref name="chunkIdx" />, merge the top-of-stack
    /// entry with the incoming CV while the number of trailing zeros in
    /// <c>(<paramref name="chunkIdx" /> + 1)</c> is greater than or equal to the current stack
    /// depth. This ensures that only perfectly balanced subtrees of equal height are merged,
    /// maintaining the left-to-right ordering of leaves.
    /// </para>
    /// </remarks>
    /// <param name="cv">The 8-word chaining value produced by the completed chunk.</param>
    /// <param name="chunkIdx">The zero-based chunk index of the completed chunk.</param>
    private void PushChunkCv(uint[] cv, ulong chunkIdx)
    {
        // Merge while the top-of-stack subtree and the incoming CV form a balanced pair.
        while (_cvStack.Count > 0 && IsSubtreeComplete(chunkIdx, _cvStack.Count))
        {
            uint[] left = _cvStack[_cvStack.Count - 1];
            _cvStack.RemoveAt(_cvStack.Count - 1);
            cv = ParentCv(left, cv, isRoot: false);
        }

        _cvStack.Add(cv);
    }

    /// <summary>
    /// Determines whether the subtrees currently on the stack and the newly completed chunk
    /// should be merged, based on the binary tree completion invariant.
    /// </summary>
    /// <param name="chunkIdx">The zero-based index of the most recently completed chunk.</param>
    /// <param name="stackDepth">The current depth of <see cref="_cvStack" />.</param>
    /// <returns>
    /// <see langword="true" /> if the top entry on the stack represents a subtree of exactly
    /// the same height as the subtree rooted at the incoming CV, and therefore the two should
    /// be merged into a parent node.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSubtreeComplete(ulong chunkIdx, int stackDepth)
    {
        // The number of trailing zeros in (chunkIdx + 1) reflects how many full subtree levels
        // are complete at this point.  Merge whenever that count meets or exceeds the stack depth.
        ulong completed = chunkIdx + 1;
        int trailingZeros = System.Numerics.BitOperations.TrailingZeroCount(completed);
        return trailingZeros >= stackDepth;
    }
}
